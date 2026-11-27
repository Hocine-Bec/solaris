Date / time:    2026-11-24T11:22:31+01:00
Git commit:     p4-before
Machine:        Micro-Star International Co., Ltd. Modern 15 H B13M · 13th Gen Intel(R) Core(TM) i7-13620H (16 threads) · 14.8 GiB RAM
OS / kernel:    Ubuntu 26.04.1 LTS · 7.0.0-44-generic
Power profile:  balanced ; on mains power ; CPU MHz during the run: 2496–3200 MHz across the cores
.NET:           10.0.102 (Release build)
PostgreSQL:     postgres:18 in Docker · PostgreSQL 18.6 (Debian 18.6-1.pgdg13+2) on x86_64-pc-linux-gnu, compiled by gcc (Debian 14.2.0-19) 14.2.0, 64-bit · configuration changes: none
Dataset:        dataset M: 1000 installations × 120 days, seed 42, naive layout; 5587272 readings
Load tool:      k6 v2.3.0 (commit/e088784614, go1.26.8, linux/amd64) · perf/p4/dashboards.js (three views, 30s warm-up then 10/s for 60s each)
Other load:     containers: solaris-postgres-1; browser: not running
Notes:          Rate limiter off. The plans are of the rows each view reads, with EXPLAIN (ANALYZE, BUFFERS), after the load.
