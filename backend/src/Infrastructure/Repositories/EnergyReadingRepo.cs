using Application.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Infrastructure.Repositories;

public class EnergyReadingRepo(AppDbContext context) : IEnergyReadingRepo
{
    /// <summary>
    /// The whole batch in one statement: the readings the installation hasn't got, and the local days they land in,
    /// written together so a day is never marked for a reading that wasn't stored, nor a reading stored without its
    /// day being marked. <c>ON CONFLICT DO NOTHING</c> is what makes a duplicate impossible rather than unlikely
    /// (BR-T1): two devices sending one slot at the same moment leave one row, and the second is simply not new.
    /// </summary>
    public async Task<int> AddBatchAsync(int installationId, string timeZone,
        IReadOnlyList<(DateTime SlotStart, int EnergyWh)> readings)
    {
        if (readings.Count == 0)
            return 0;

        var slots = readings.Select(r => r.SlotStart).ToArray();
        var wh = readings.Select(r => r.EnergyWh).ToArray();
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        var opened = connection.State != System.Data.ConnectionState.Open;
        if (opened)
            await connection.OpenAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                WITH inserted AS (
                  INSERT INTO energy_readings (installation_id, slot_start, energy_wh)
                  SELECT @installationId, s, w FROM unnest(@slots, @wh) AS t(s, w)
                  ON CONFLICT (installation_id, slot_start) DO NOTHING
                  RETURNING slot_start
                ), dirty AS (
                  INSERT INTO production_dirty_days (installation_id, day)
                  SELECT DISTINCT @installationId, (slot_start AT TIME ZONE @timeZone)::date FROM inserted
                  ON CONFLICT DO NOTHING
                )
                SELECT count(*) FROM inserted
                """;
            if (context.Database.CurrentTransaction is { } transaction)
                command.Transaction = (NpgsqlTransaction)transaction.GetDbTransaction();
            command.Parameters.AddWithValue("installationId", installationId);
            command.Parameters.AddWithValue("timeZone", timeZone);
            command.Parameters.AddWithValue("slots", slots);
            command.Parameters.AddWithValue("wh", wh);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }
        finally
        {
            if (opened)
                await connection.CloseAsync();
        }
    }
}
