
# DB folder

The **`DB`** folder is the common location of `file-system` databases used by `FlioxHub` instances of an application.

Each database is a folder in the **`DB`** folder. Common databases are:
- `main_db` - storing domain specific data for an application
- `user_db` - storing user permissions used for authorization

## Use cases
Using `file-system` databases enables:

- Creating **proof-of-concept** database applications without any 3rd party dependencies
- Suitable for **TDD** as test records are JSON files versioned via Git and providing access to their change history
- Using a database **without configuration** by using a relative database path within a project
- View and edit database records as JSON files with **text editors** like VSCode, vi, web browsers, ...
- Using a `file-system` database as source to **seed** other databases



