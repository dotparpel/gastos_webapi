USE [master]
GO
/****** Object:  Database [expenses_db]    Script Date: 14/10/2023 14:24:26 ******/
CREATE DATABASE [expenses_db]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'expenses_db', FILENAME = N'/var/opt/mssql/data/expenses_db.mdf' , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'expenses_db_log', FILENAME = N'/var/opt/mssql/data/expenses_db_log.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF
GO
ALTER DATABASE [expenses_db] SET COMPATIBILITY_LEVEL = 160
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [expenses_db].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [expenses_db] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [expenses_db] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [expenses_db] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [expenses_db] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [expenses_db] SET ARITHABORT OFF 
GO
ALTER DATABASE [expenses_db] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [expenses_db] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [expenses_db] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [expenses_db] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [expenses_db] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [expenses_db] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [expenses_db] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [expenses_db] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [expenses_db] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [expenses_db] SET  DISABLE_BROKER 
GO
ALTER DATABASE [expenses_db] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [expenses_db] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [expenses_db] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [expenses_db] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [expenses_db] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [expenses_db] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [expenses_db] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [expenses_db] SET RECOVERY FULL 
GO
ALTER DATABASE [expenses_db] SET  MULTI_USER 
GO
ALTER DATABASE [expenses_db] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [expenses_db] SET DB_CHAINING OFF 
GO
ALTER DATABASE [expenses_db] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [expenses_db] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [expenses_db] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [expenses_db] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
EXEC sys.sp_db_vardecimal_storage_format N'expenses_db', N'ON'
GO
ALTER DATABASE [expenses_db] SET QUERY_STORE = ON
GO
ALTER DATABASE [expenses_db] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
USE [expenses_db]
GO

/* Login. */
CREATE LOGIN [expenseuser] WITH PASSWORD='expensepwd'
    , DEFAULT_DATABASE = [expenses_DB]
    , CHECK_EXPIRATION=OFF
    , CHECK_POLICY=OFF

/****** Object:  User [expenseuser]    Script Date: 14/10/2023 14:24:26 ******/
CREATE USER [expenseuser] FOR LOGIN [expenseuser] WITH DEFAULT_SCHEMA=[dbo]
GO
ALTER ROLE [db_owner] ADD MEMBER [expenseuser]
GO
/****** Object:  Table [dbo].[d_category]    Script Date: 14/10/2023 14:24:26 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[d_category](
	[cat_id] [int] IDENTITY(1,1) NOT NULL,
	[cat_desc] [nvarchar](64) NOT NULL,
	[cat_order] [int] NULL,
 CONSTRAINT [PK_d_category] PRIMARY KEY CLUSTERED 
(
	[cat_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[t_expense]    Script Date: 14/10/2023 14:24:26 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[t_expense](
	[expense_id] [int] IDENTITY(1,1) NOT NULL,
	[expense_date] [datetimeoffset](7) NOT NULL,
	[expense_desc] [nvarchar](128) NULL,
	[expense_amount] [numeric](12, 2) NOT NULL,
	[cat_id] [int] NULL,
 CONSTRAINT [PK_t_expense] PRIMARY KEY CLUSTERED 
(
	[expense_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[v_expense]    Script Date: 14/10/2023 14:24:26 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[v_expense] AS
SELECT expense_id 
    , expense_date
    , expense_desc
    , expense_amount
    , expense.cat_id
    , cat.cat_desc
FROM t_expense AS expense
	LEFT JOIN d_category AS cat
		ON expense.cat_id = cat.cat_id
GO
/****** Object:  View [dbo].[v_expense_report]    Script Date: 14/10/2023 14:24:26 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[v_expense_report] AS
SELECT expense_id 
    , expense_date
    , expense_desc
    , expense_amount
    , expense.cat_id
    , cat.cat_desc
	, YEAR(expense_date) AS year
	, MONTH(expense_date) AS month
	, DATEPART(week,  expense_date) AS week
	, DAY(expense_date) AS day
FROM t_expense AS expense
	LEFT JOIN d_category AS cat
		ON expense.cat_id = cat.cat_id
GO
/****** Object:  Table [dbo].[c_config]    Script Date: 14/10/2023 14:24:26 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[c_config](
	[config_key] [nvarchar](64) NOT NULL,
	[config_value] [nvarchar](64) NULL,
 CONSTRAINT [PK_c_config] PRIMARY KEY CLUSTERED 
(
	[config_key] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[d_user]    Script Date: 14/10/2023 14:24:26 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[d_user](
	[user_id] [int] IDENTITY(1,1) NOT NULL,
	[user_login] [nvarchar](128) NOT NULL,
	[user_pwd] [nvarchar](128) NOT NULL,
	[user_access_key] [uniqueidentifier] NULL,
	[user_access_token_expire_minutes] [numeric](12, 2) NULL,
	[user_refresh_key] [varchar](128) NULL,
	[user_refresh_token_expire_minutes] [numeric](12, 2) NULL,
	[user_refresh_expire_date] [datetimeoffset](7) NULL,
	[user_login_expire_date] [datetimeoffset](7) NULL,
 CONSTRAINT [PK_d_user] PRIMARY KEY CLUSTERED 
(
	[user_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [i_category_cat_desc]    Script Date: 14/10/2023 14:24:26 ******/
CREATE NONCLUSTERED INDEX [i_category_cat_desc] ON [dbo].[d_category]
(
	[cat_desc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [i_category_cat_order]    Script Date: 14/10/2023 14:24:26 ******/
CREATE NONCLUSTERED INDEX [i_category_cat_order] ON [dbo].[d_category]
(
	[cat_order] ASC,
	[cat_desc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [i_user_access_key]    Script Date: 14/10/2023 14:24:26 ******/
CREATE NONCLUSTERED INDEX [i_user_access_key] ON [dbo].[d_user]
(
	[user_access_key] ASC,
	[user_login_expire_date] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [i_user_login_pwd]    Script Date: 14/10/2023 14:24:26 ******/
CREATE NONCLUSTERED INDEX [i_user_login_pwd] ON [dbo].[d_user]
(
	[user_login] ASC,
	[user_pwd] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [i_expense_expense_date]    Script Date: 14/10/2023 14:24:26 ******/
CREATE NONCLUSTERED INDEX [i_expense_expense_date] ON [dbo].[t_expense]
(
	[expense_date] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[t_expense]  WITH CHECK ADD  CONSTRAINT [FK_t_expense_category] FOREIGN KEY([cat_id])
REFERENCES [dbo].[d_category] ([cat_id])
GO
ALTER TABLE [dbo].[t_expense] CHECK CONSTRAINT [FK_t_expense_category]
GO
USE [master]
GO
ALTER DATABASE [expenses_db] SET  READ_WRITE 
GO

-- Create an initial user
IF (SELECT COUNT(*) FROM d_user WHERE user_login = 'admin') = 0 BEGIN
  INSERT INTO d_user(user_login, user_pwd)
  VALUES('admin', 'CZfxoPXlBrg7qcUA4Y2JQg==');
END