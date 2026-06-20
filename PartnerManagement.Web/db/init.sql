IF DB_ID(N'PartnerManagementDb') IS NULL
BEGIN
    CREATE DATABASE PartnerManagementDb;
END
GO

USE PartnerManagementDb;
GO

IF OBJECT_ID(N'dbo.InsurancePolicies', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.InsurancePolicies;
END
GO

IF OBJECT_ID(N'dbo.Partners', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Partners;
END
GO

CREATE TABLE dbo.Partners
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Partners PRIMARY KEY,
    FirstName NVARCHAR(255) NOT NULL,
    LastName NVARCHAR(255) NOT NULL,
    Address NVARCHAR(255) NULL,
    PartnerNumber CHAR(20) NOT NULL,
    CroatianPIN CHAR(11) NULL,
    PartnerTypeId INT NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Partners_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    CreatedByUser NVARCHAR(255) NOT NULL,
    IsForeign BIT NOT NULL,
    ExternalCode NVARCHAR(20) NOT NULL,
    Gender CHAR(1) NOT NULL,
    CONSTRAINT UQ_Partners_PartnerNumber UNIQUE (PartnerNumber),
    CONSTRAINT UQ_Partners_ExternalCode UNIQUE (ExternalCode),
    CONSTRAINT UQ_Partners_CroatianPIN UNIQUE (CroatianPIN),
    CONSTRAINT CK_Partners_PartnerTypeId CHECK (PartnerTypeId IN (1, 2)),
    CONSTRAINT CK_Partners_Gender CHECK (Gender IN ('M', 'F', 'N')),
    CONSTRAINT CK_Partners_PartnerNumberDigits CHECK (PartnerNumber NOT LIKE '%[^0-9]%'),
    CONSTRAINT CK_Partners_FirstNameLength CHECK (LEN(FirstName) BETWEEN 2 AND 255),
    CONSTRAINT CK_Partners_LastNameLength CHECK (LEN(LastName) BETWEEN 2 AND 255),
    CONSTRAINT CK_Partners_ExternalCodeLength CHECK (LEN(ExternalCode) BETWEEN 10 AND 20)
);
GO

CREATE TABLE dbo.InsurancePolicies
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InsurancePolicies PRIMARY KEY,
    PartnerId INT NOT NULL,
    PolicyNumber NVARCHAR(15) NOT NULL,
    PolicyAmount DECIMAL(18,2) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_InsurancePolicies_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_InsurancePolicies_Partners FOREIGN KEY (PartnerId) REFERENCES dbo.Partners(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_InsurancePolicies_PolicyNumber UNIQUE (PolicyNumber),
    CONSTRAINT CK_InsurancePolicies_PolicyNumberLength CHECK (LEN(PolicyNumber) BETWEEN 10 AND 15),
    CONSTRAINT CK_InsurancePolicies_PolicyAmount CHECK (PolicyAmount > 0)
);
GO