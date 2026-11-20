Date / time:    2026-11-20T15:51:33+01:00
Git commit:     p1-before-81-g495754b
Machine:        Micro-Star International Co., Ltd. Modern 15 H B13M · 13th Gen Intel(R) Core(TM) i7-13620H (16 threads) · 14.8 GiB RAM
OS / kernel:    Ubuntu 26.04.1 LTS · 7.0.0-43-generic
Power profile:  balanced ; on mains power ; CPU MHz during the run: 400–3800 MHz across the cores
.NET:           10.0.102 (Release build)
PostgreSQL:     postgres:18 in Docker · PostgreSQL 18.6 (Debian 18.6-1.pgdg13+2) on x86_64-pc-linux-gnu, compiled by gcc (Debian 14.2.0-19) 14.2.0, 64-bit · configuration changes: none
Dataset:        an empty database; the run creates its own leads
Load tool:      k6 v2.3.0 (commit/e088784614, go1.26.8, linux/amd64) · perf/p2/leads.js (scenario B, 500 leads at 20/s)
Other load:     containers: solaris-postgres-1; browser: not running
Notes:          Recording sender, 2 s per email; the outbox, 20 sends at once. Drain time measured from the restart.
