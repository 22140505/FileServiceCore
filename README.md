<p align="center">
    <span>English</span> |  
    <a href="README.zh-CN.md">中文</a>
</p>

# introduction
Simple, stable and easy-to-use file service

Unlike the file system based file service program, this program directly uses the sqlserver database as the storage mode, and has obvious advantages:

- You can use SQL Server's own backup and fault recovery strategies
- Support transactions

FileServiceCore has a basic cache and a simple authentication mechanism built in


# usage
Create a database named fileservicecore. Execute sql script file: db_gen.sql
Configure connstr and appid in appsettings.json. 
Generate and then launch FileServiceCore.exe
The program should now be listening on port 28001
You can test apis at http://localhost:28001/swagger/

These interfaces are provided. The specific parameters can be seen in the code comments

- /v1/upload
- /v1/delete
- /v1/query
- /v1/download

The first three apis need to be authenticated by adding an item named appid to the headers of the request.
This appid value is configured in appsettings.json
