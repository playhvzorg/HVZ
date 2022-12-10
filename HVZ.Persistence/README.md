This project simply defines repository interfaces for all [Models](../HVZ.Model) that can be persisted,
without imposing how the persistence may be implemented.
This realizes the [Data Access Object pattern](https://en.wikipedia.org/wiki/Data_access_object).

Application code is advised to only depend on classes from this project
 and not any concrete implementation.

Functions in this space are defined under the following name convention:
FindX may return null if X is not found.
GetX will throw an error if X is not found.
Most code should avoid using FindX unless it is unclear if X exists.