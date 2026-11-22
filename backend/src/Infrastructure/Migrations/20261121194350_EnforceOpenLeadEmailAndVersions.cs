using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceOpenLeadEmailAndVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "normalized_email",
                table: "leads",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            // Existing leads get their email as it is compared (Emails.Normalize: trimmed, upper-cased), as customers
            // did; open leads that already share one stop the migration, naming them, for someone to close all but one.
            migrationBuilder.Sql("UPDATE leads SET normalized_email = upper(btrim(email));");
            migrationBuilder.Sql(StopOnSharedOpenEmailsSql);

            migrationBuilder.CreateIndex(
                name: "ux_leads_open_normalized_email",
                table: "leads",
                column: "normalized_email",
                unique: true,
                filter: "status IN ('New', 'Contacted')");

            // Leads and installations use PostgreSQL's xmin as their version: a system column, so there is nothing to
            // add for it.

            migrationBuilder.Sql(RehashAddressesSql);
        }

        /// <summary>Stops the migration when open leads share an email, naming their IDs.</summary>
        public const string StopOnSharedOpenEmailsSql = """
            DO $$
            DECLARE shared text;
            BEGIN
                SELECT string_agg(ids, '; ') INTO shared
                FROM (SELECT string_agg(id::text, ', ' ORDER BY id) AS ids
                      FROM leads
                      WHERE status IN ('New', 'Contacted')
                      GROUP BY normalized_email
                      HAVING count(*) > 1) AS same_email;
                IF shared IS NOT NULL THEN
                    RAISE EXCEPTION 'Open leads share an email (BR-L1): leads %. Close all but one of each, then migrate again.', shared;
                END IF;
            END $$;
            """;

        /// <summary>
        /// An address's key now ignores white space around and inside its fields (Address.BuildHash): the addresses
        /// whose fields have any are given their new key; the others' doesn't change. It is computed as BuildHash
        /// computes it (each field trimmed and its runs of white space made one space, joined with '|', upper-cased,
        /// SHA-256 as upper-case hex), for addresses in ASCII, where upper() and ToUpperInvariant agree. If two
        /// addresses become one key, the migration stops, naming them.
        /// </summary>
        public const string RehashAddressesSql = """
            DO $$
            DECLARE clash text;
            BEGIN
                CREATE TEMP TABLE rehashed ON COMMIT DROP AS
                SELECT id, upper(encode(sha256(convert_to(upper(concat_ws('|', s, c, st, z, co)), 'UTF8')), 'hex')) AS hash
                FROM (SELECT id,
                             regexp_replace(regexp_replace(street, '^\s+|\s+$', '', 'g'), '\s+', ' ', 'g') AS s,
                             regexp_replace(regexp_replace(city, '^\s+|\s+$', '', 'g'), '\s+', ' ', 'g') AS c,
                             regexp_replace(regexp_replace(state, '^\s+|\s+$', '', 'g'), '\s+', ' ', 'g') AS st,
                             regexp_replace(regexp_replace(zip_code, '^\s+|\s+$', '', 'g'), '\s+', ' ', 'g') AS z,
                             regexp_replace(regexp_replace(country, '^\s+|\s+$', '', 'g'), '\s+', ' ', 'g') AS co,
                             street, city, state, zip_code, country
                      FROM addresses) AS tidy
                WHERE (s, c, st, z, co) IS DISTINCT FROM (street, city, state, zip_code, country);

                SELECT string_agg(r.id || ' and ' || a.id, '; ') INTO clash
                FROM rehashed r JOIN addresses a ON a.unique_address_hash = r.hash AND a.id <> r.id;
                IF clash IS NULL THEN
                    SELECT string_agg(ids, '; ') INTO clash
                    FROM (SELECT string_agg(id::text, ' and ' ORDER BY id) AS ids
                          FROM rehashed GROUP BY hash HAVING count(*) > 1) AS same;
                END IF;
                IF clash IS NOT NULL THEN
                    RAISE EXCEPTION 'Addresses become the same address once white space is ignored: %. Merge them, then migrate again.', clash;
                END IF;

                UPDATE addresses a SET unique_address_hash = r.hash FROM rehashed r WHERE a.id = r.id;
            END $$;
            """;

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_leads_open_normalized_email",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "normalized_email",
                table: "leads");

            // The addresses keep their new keys: the old BuildHash is gone, and a key only has to be unique
        }
    }
}
