USE [fileservicecore]
GO
/****** Object:  Table [dbo].[claim]    Script Date: 2022/11/16 10:38:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[claim](
	[autoid] [int] IDENTITY(1,1) NOT NULL,
	[fileId] [char](32) NOT NULL,
	[name] [varchar](50) NOT NULL,
	[value] [varchar](50) NOT NULL,
 CONSTRAINT [PK_claim] PRIMARY KEY CLUSTERED 
(
	[autoid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[file]    Script Date: 2022/11/16 10:38:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[file](
	[autoid] [int] IDENTITY(1,1) NOT NULL,
	[id] [char](32) NOT NULL,
	[appid] [char](32) NOT NULL,
	[index] [varchar](128) NOT NULL,
	[name] [nvarchar](255) NOT NULL,
	[content] [varbinary](max) NOT NULL,
	[length] [bigint] NOT NULL,
	[hash] [binary](16) NOT NULL,
	[date] [datetime] NOT NULL,
	[claims] [varchar](500) NULL,
 CONSTRAINT [PK_file] PRIMARY KEY CLUSTERED 
(
	[autoid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
