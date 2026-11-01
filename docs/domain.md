# Domain

The business Solaris serves, the words it uses, the people involved, and the journey from a quote request to
a monitored solar system. The requirements, the architecture and the code all use this vocabulary.

---

## The business

**Solaris Energy** is a fictional company that sells and installs solar-panel systems on homes and small
businesses in **Austin, Texas**. Addresses are therefore US addresses, and expected production is based on
Austin's sunlight.

A homeowner pays once for a system: panels on the roof, an inverter, sometimes a battery. The system then
produces electricity for 25 years or more, lowering the homeowner's bill. The company earns from the sale
and the installation, and keeps a relationship with the customer afterwards: monitoring, repairs, warranty
work.

The company needs a CRM for three reasons:

1. **Many people are interested; few buy.** The website's quote form produces many leads, and sales must
   contact each one quickly: a slow reply loses the customer to a competitor.
2. **An installation is a project that takes weeks:** a site visit, a design, permits from the city, the
   physical installation, an inspection, and switching the system on. Several people work on each project,
   and the office needs to see where every project stands and what blocks it.
3. **After switch-on, every system reports its production all day.** The company must notice when a system
   underperforms: a broken inverter on a customer's roof is lost money and an unhappy customer.

Solaris covers all three, deliberately small.

---

## Glossary

| Term | Meaning |
|---|---|
| **Lead** | A person who asked for a quote and hasn't bought yet. |
| **Customer** | A lead who agreed to go ahead. Created by converting a lead. |
| **Installation** | One solar project for one customer at one address, from site visit to a working system. |
| **Solar panel (PV module)** | Converts sunlight into electricity. A home system has roughly 15–30 panels. |
| **Inverter** | Converts the panels' DC electricity into the home's AC electricity. It is usually the device that reports production data, and the part that fails most often. |
| **kW (kilowatt)** | Power: how fast electricity is produced at one moment. A system's **size** is its maximum power, e.g. an 8 kW system. |
| **kWh (kilowatt-hour)** | Energy: the amount produced over time. 8 kW for 1 hour = 8 kWh. Bills and production are counted in kWh. |
| **Peak sun hours** | The hours per day of "full" sunlight equivalent at a location. Austin averages about 5, more in summer and fewer in winter. |
| **Performance ratio** | The share of theoretical output a real system delivers after losses from heat, dust, wiring and the inverter: typically about 0.80. |
| **Expected production** | What a healthy system should produce: size × peak sun hours × performance ratio. |
| **Site survey** | A technician's visit to the property: roof condition, orientation, shading, the electrical panel. |
| **Design** | Engineering the system: how many panels, which inverter, the system size. |
| **Permit** | The city's approval to install. It can take weeks. |
| **Interconnection** | The utility's approval to connect the system to the grid. |
| **Inspection** | A city or utility inspector checks that the finished installation is safe and compliant. |
| **Activation** | Switching the system on and starting monitoring. The installation becomes **Active**. |
| **Reading** | A data point sent by the inverter's monitoring gateway: the energy produced in one 15-minute slot. |
| **Rollup** | A pre-computed daily total built from many readings, so dashboards don't add up millions of rows. |
| **Health** | A label (Excellent … Offline) comparing a day's actual production with its expected production. |

---

## People and roles

| Who | Role in Solaris | Can do |
|---|---|---|
| Visitor, prospect | **Public** (no login) | Submit the quote form on the marketing site |
| Office staff, sales, project manager | **Admin** | Everything: leads, customers, installations, users, dashboards, failed notifications |
| Field technician (surveyor, installer, inspector) | **Technician** | See the installations they are assigned to; mark their own assignment complete |
| Inverter monitoring gateway | **Device** (an API key, not a user) | Upload production readings for its own installation only |
| Customer (homeowner) | Not a user | Receives emails; there is no customer portal |

---

## The journey

```
Website form → Lead → (sales contacts them) → Converted
                                                 ↓
                               Customer (Prospect) + Installation (Survey)
                                                 ↓
             Survey → Design → Permits → Installation → Inspection → Active
                                                                       ↓
                                       readings every 15 min → daily rollup → health
```

| Stage | What happens | What Solaris records |
|---|---|---|
| Lead | The person fills in the quote form: contact, property, monthly bill range | A lead with status New; a welcome email to them and an alert to sales |
| Contacted | Sales calls or emails them | Status Contacted, with a timestamp |
| Converted | They agree to a site visit | A customer (Prospect) and an installation (Survey) at the lead's address, created together |
| Disqualified / Lost | Not a fit (a renter, an unsuitable roof), or they chose a competitor | A final status; the lead is closed |
| Survey | A surveyor visits the site | A surveyor assignment, marked complete when done |
| Design | Engineering decides the system | System size (kW), panel count, inverter type |
| Permits | The city and the utility approve | The status only (documents are out of scope) |
| Installation | A crew installs the system | Installer assignments, each marked complete when done |
| Inspection | The inspector checks the work | An inspector assignment, marked complete when passed |
| Active | Switched on and monitored | The completion date, a device API key, an email to the customer |
| Monitoring | The device reports every 15 minutes | Readings → a daily rollup → a health label |
| Deactivated | The system is removed or decommissioned | A final status; the device key is revoked |
| Cancelled | The customer drops out before activation | A final status, with a reason |
