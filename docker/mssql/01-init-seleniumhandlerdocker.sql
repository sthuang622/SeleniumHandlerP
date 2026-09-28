IF DB_ID(N'seleniumhandlerdocker') IS NULL
BEGIN
    CREATE DATABASE seleniumhandlerdocker;
END;
GO

USE seleniumhandlerdocker;
GO

IF OBJECT_ID(N'dbo.FileConfigResult', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FileConfigResult
    (
        Id INT IDENTITY(1,1) NOT NULL,
        RunId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_FileConfigResult_RunId DEFAULT NEWID(),
        Name NVARCHAR(256) NOT NULL,
        Description NVARCHAR(1024) NULL,
        FilePath NVARCHAR(1024) NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_FileConfigResult_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_FileConfigResult PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF COL_LENGTH(N'dbo.FileConfigResult', N'RunId') IS NULL
BEGIN
    ALTER TABLE dbo.FileConfigResult
    ADD RunId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_FileConfigResult_RunId DEFAULT NEWID();
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_FileConfigResult_RunId'
      AND object_id = OBJECT_ID(N'dbo.FileConfigResult')
)
BEGIN
    CREATE INDEX IX_FileConfigResult_RunId
    ON dbo.FileConfigResult (RunId);
END;
GO

IF OBJECT_ID(N'dbo.TestResult', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TestResult
    (
        Id INT IDENTITY(1,1) NOT NULL,
        Step INT NOT NULL CONSTRAINT DF_TestResult_Step DEFAULT 0,
        Name NVARCHAR(256) NOT NULL,
        Description NVARCHAR(1024) NULL,
        FilePath NVARCHAR(1024) NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_TestResult_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdateTime DATETIME2(0) NOT NULL CONSTRAINT DF_TestResult_UpdateTime DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_TestResult PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF COL_LENGTH(N'dbo.TestResult', N'Step') IS NULL
BEGIN
    ALTER TABLE dbo.TestResult
    ADD Step INT NOT NULL CONSTRAINT DF_TestResult_Step DEFAULT 0;
END;
GO

IF COL_LENGTH(N'dbo.TestResult', N'Name') IS NULL
BEGIN
    ALTER TABLE dbo.TestResult
    ADD Name NVARCHAR(256) NOT NULL CONSTRAINT DF_TestResult_Name DEFAULT N'';
END;
GO

IF COL_LENGTH(N'dbo.TestResult', N'Description') IS NULL
BEGIN
    ALTER TABLE dbo.TestResult
    ADD Description NVARCHAR(1024) NULL;
END;
GO

IF COL_LENGTH(N'dbo.TestResult', N'UpdateTime') IS NULL
BEGIN
    ALTER TABLE dbo.TestResult
    ADD UpdateTime DATETIME2(0) NOT NULL CONSTRAINT DF_TestResult_UpdateTime DEFAULT SYSUTCDATETIME();
END;
GO

IF OBJECT_ID(N'dbo.StepResult', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StepResult
    (
        Id INT IDENTITY(1,1) NOT NULL,
        TestResultId INT NOT NULL,
        Step INT NOT NULL,
        Name NVARCHAR(256) NOT NULL,
        Description NVARCHAR(1024) NULL,
        Screenshot NVARCHAR(1024) NULL,
        Result NVARCHAR(50) NOT NULL CONSTRAINT DF_StepResult_Result DEFAULT N'Not Run',
        ErrorMessage NVARCHAR(2048) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_StepResult_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdateTime DATETIME2(0) NOT NULL CONSTRAINT DF_StepResult_UpdateTime DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_StepResult PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_StepResult_TestResult
            FOREIGN KEY (TestResultId)
            REFERENCES dbo.TestResult (Id)
            ON DELETE CASCADE
    );
END;
GO

IF COL_LENGTH(N'dbo.StepResult', N'Step') IS NULL
BEGIN
    ALTER TABLE dbo.StepResult
    ADD Step INT NOT NULL CONSTRAINT DF_StepResult_Step DEFAULT 0;
END;
GO

IF COL_LENGTH(N'dbo.StepResult', N'Name') IS NULL
BEGIN
    ALTER TABLE dbo.StepResult
    ADD Name NVARCHAR(256) NOT NULL CONSTRAINT DF_StepResult_Name DEFAULT N'';
END;
GO

IF COL_LENGTH(N'dbo.StepResult', N'Description') IS NULL
BEGIN
    ALTER TABLE dbo.StepResult
    ADD Description NVARCHAR(1024) NULL;
END;
GO

IF COL_LENGTH(N'dbo.StepResult', N'ErrorMessage') IS NULL
BEGIN
    ALTER TABLE dbo.StepResult
    ADD ErrorMessage NVARCHAR(2048) NULL;
END;
GO

IF COL_LENGTH(N'dbo.StepResult', N'UpdateTime') IS NULL
BEGIN
    ALTER TABLE dbo.StepResult
    ADD UpdateTime DATETIME2(0) NOT NULL CONSTRAINT DF_StepResult_UpdateTime DEFAULT SYSUTCDATETIME();
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.default_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.StepResult')
      AND parent_column_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.StepResult'), N'Result', N'ColumnId')
)
BEGIN
    ALTER TABLE dbo.StepResult
    ADD CONSTRAINT DF_StepResult_Result DEFAULT N'Not Run' FOR Result;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_StepResult_TestResultId'
      AND object_id = OBJECT_ID(N'dbo.StepResult')
)
BEGIN
    CREATE INDEX IX_StepResult_TestResultId
    ON dbo.StepResult (TestResultId);
END;
GO

IF COL_LENGTH(N'dbo.TestResult', N'FileConfigResultId') IS NULL
BEGIN
    ALTER TABLE dbo.TestResult
    ADD FileConfigResultId INT NULL;
END;
GO

IF OBJECT_ID(N'dbo.FK_TestResult_FileConfigResult', N'F') IS NULL
BEGIN
    ALTER TABLE dbo.TestResult
    ADD CONSTRAINT FK_TestResult_FileConfigResult
        FOREIGN KEY (FileConfigResultId)
        REFERENCES dbo.FileConfigResult (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_TestResult_FileConfigResultId'
      AND object_id = OBJECT_ID(N'dbo.TestResult')
)
BEGIN
    CREATE INDEX IX_TestResult_FileConfigResultId
    ON dbo.TestResult (FileConfigResultId);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_TestResult_FileConfigResultId_Step_Id'
      AND object_id = OBJECT_ID(N'dbo.TestResult')
)
BEGIN
    CREATE INDEX IX_TestResult_FileConfigResultId_Step_Id
    ON dbo.TestResult (FileConfigResultId, Step, Id);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_StepResult_TestResultId_Step_Id'
      AND object_id = OBJECT_ID(N'dbo.StepResult')
)
BEGIN
    CREATE INDEX IX_StepResult_TestResultId_Step_Id
    ON dbo.StepResult (TestResultId, Step, Id);
END;
GO
