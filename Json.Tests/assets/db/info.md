

This folder contains the database used by the unit tests located in [Flow/Graph unit tests](../../Common/UnitTest/Flow/Graph).

The schema of the database is describes via the models in [PocStore.cs](../../Common/UnitTest/Flow/Graph/PocStore.cs).

Each `EntitySet<>` field in `PocStore` describe the model used in each folder aka container.
This means that all payloads in a container folder are of a specific type.
These are `Article`, `Customer`, `Employee`, `Order` and `Producer`.



