# Solaris

A CRM backend for a fictional solar-panel installer: from a website quote request to a running solar
system, then that system's daily electricity production.

The focus is on staying correct when things go wrong:

- notifications that are never lost, even when the mail server is down or the API crashes
- no duplicate records and no lost updates when people and devices act at the same time
- production dashboards that stay fast with more than 10 million telemetry readings

Each of these is built naively first, measured, fixed, and measured again. The results will be published
here with the method to reproduce them.

## Status

Version 1 is being built on top of an earlier version, in which lead capture was the only feature working
end to end: a React quote form posts to the API, which stores the lead in PostgreSQL and emails the lead and
the sales team. That code is being brought up to standard first; the rest of v1 is described in the
documentation below. No measurements have been published yet, and setup instructions will come with the
Docker setup.

## Stack

ASP.NET Core (.NET 9, moving to .NET 10) · EF Core · PostgreSQL · Hangfire · MailKit ·
React + TypeScript + Vite (the marketing site and quote form)

## Documentation

- [Domain](docs/domain.md): the business, its vocabulary, the people involved and the customer journey
- [Functional requirements](docs/functional-requirements.md): what the system must do, and its business rules
- [Non-functional requirements](docs/non-functional-requirements.md): the scale and the quality targets
- [Architecture](docs/architecture.md): how the system is built
- [Architecture decisions](docs/adr/): one record per decision, with its options and trade-offs
- [Testing](docs/testing.md): what is tested, at which level, and how
- [Operations](docs/operations.md): configuration, running, CI and deployment
- [Measurements](docs/measurements.md): the four measured problems, their method and their results

## License

[MIT](LICENSE)
