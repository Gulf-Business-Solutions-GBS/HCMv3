/* =====================================================================
   01 - CREATE DATABASE AND APPLICATION LOGIN
   ---------------------------------------------------------------------
   Run this FIRST, in SQL Server Management Studio, connected as sa
   (or another sysadmin login), with the query window set to "master".

   Run 02-schema-all-migrations.sql afterwards.

   This server has a single C: volume, so the database files live on C:.
   That is safe here because SQL Server Express refuses to grow any
   database past 10 GB, and the log file is capped at 8 GB by MAXSIZE
   below. Worst case on disk is about 18 GB - ensure 25 GB is free.

   The application password is NOT in this file, because this copy lives
   in the source repository. Replace REPLACE_WITH_APP_PASSWORD below with
   the password that the packaging step also writes into the connection
   string in api\appsettings.Production.json - the two must match. See
   ..\README.md for where that password is kept.
   ===================================================================== */

USE [master];
GO

/* ---------------------------------------------------------------------
   1. Folders for the database files.
      Kept out of the default SQL install directory so they are easy to
      find, back up, and exclude from antivirus scanning.
   --------------------------------------------------------------------- */
DECLARE @DataPath NVARCHAR(260) = N'C:\SQLData\';
DECLARE @LogPath  NVARCHAR(260) = N'C:\SQLLogs\';

EXEC master.dbo.xp_create_subdir @DataPath;
EXEC master.dbo.xp_create_subdir @LogPath;
GO

/* ---------------------------------------------------------------------
   2. Create the database.
      SIMPLE recovery: the transaction log is reclaimed automatically.
      This is correct for a non-production server. Change to FULL only
      when point-in-time restore is genuinely required, and only if a
      log backup job exists - otherwise the log will grow without limit.
   --------------------------------------------------------------------- */
IF DB_ID(N'HCMv3Db') IS NOT NULL
BEGIN
    PRINT 'Database HCMv3Db already exists - skipping creation.';
END
ELSE
BEGIN
    CREATE DATABASE [HCMv3Db]
    ON PRIMARY
    (
        NAME      = N'HCMv3Db',
        FILENAME  = N'C:\SQLData\HCMv3Db.mdf',
        SIZE      = 2048MB,
        FILEGROWTH = 512MB
    )
    LOG ON
    (
        NAME      = N'HCMv3Db_log',
        FILENAME  = N'C:\SQLLogs\HCMv3Db_log.ldf',
        SIZE      = 1024MB,
        FILEGROWTH = 256MB,
        MAXSIZE   = 8192MB
    );

    PRINT 'Database HCMv3Db created on C:.';
END
GO

ALTER DATABASE [HCMv3Db] SET RECOVERY SIMPLE;
ALTER DATABASE [HCMv3Db] SET AUTO_CLOSE OFF;
ALTER DATABASE [HCMv3Db] SET AUTO_SHRINK OFF;
ALTER DATABASE [HCMv3Db] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO

/* ---------------------------------------------------------------------
   3. Dedicated application login.

      The application does NOT use 'sa'. This login can only see this
      one database, so a mistyped connection string cannot damage
      anything else on the instance.
   --------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'hcm_app')
BEGIN
    CREATE LOGIN [hcm_app]
        WITH PASSWORD       = N'REPLACE_WITH_APP_PASSWORD',
             CHECK_POLICY   = ON,
             DEFAULT_DATABASE = [HCMv3Db];
    PRINT 'Login hcm_app created.';
END
ELSE
BEGIN
    PRINT 'Login hcm_app already exists - skipping.';
END
GO

USE [HCMv3Db];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'hcm_app')
BEGIN
    CREATE USER [hcm_app] FOR LOGIN [hcm_app];
    ALTER ROLE [db_owner] ADD MEMBER [hcm_app];
    PRINT 'User hcm_app mapped to HCMv3Db as db_owner.';
END
GO

/* ---------------------------------------------------------------------
   4. Confirmation
   --------------------------------------------------------------------- */
SELECT
    DB_NAME()                                   AS [Database],
    (SELECT recovery_model_desc
       FROM sys.databases
      WHERE name = N'HCMv3Db')                  AS [RecoveryModel],
    (SELECT COUNT(*)
       FROM sys.database_principals
      WHERE name = N'hcm_app')                  AS [AppUserExists];
GO

PRINT '';
PRINT '=====================================================';
PRINT ' Step 01 complete.';
PRINT ' Next: run 02-schema-all-migrations.sql against HCMv3Db';
PRINT '=====================================================';
GO
