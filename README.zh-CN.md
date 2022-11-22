<p align="center">
    <a href="README.md">English</a> |   
    <span>中文</span>
</p>

# 介绍
稳定易用的文件服务

不同于其他基于文件系统的文件服务，这个程序直接使用sqlserver数据库存储文件内容，这样就会有这些优势：

- 可以利用数据库的备份策略和错误恢复功能
- 完善的事物支持

FileServiceCore 还自带了一个简易的缓存模块，你可以轻易的将他替换为更复杂的缓存模块


# 用法
创建一个名为fileservicecore的数据库，然后执行 db_gen.sql以生成数据表。\
在appsettings.json 中配置 connstr \
生成然后启动 FileServiceCore.exe\
现在程序应该在监听 28001 端口了，你可以访问 http://localhost:28001/swagger/ 以查看api

目前提供了这些api, 在 /Controller/V1Controller.cs 中可以看到参数的说明

- /v1/upload
- /v1/delete
- /v1/query
- /v1/download

前3个api需要在请求头中添加appid作为身份验证。appid的值可以在appsettings.json更改
