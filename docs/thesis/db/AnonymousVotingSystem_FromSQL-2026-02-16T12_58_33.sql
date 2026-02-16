
CREATE TABLE __EFMigrationsHistory
(
  MigrationId    nvarchar(150) NOT NULL,
  ProductVersion nvarchar(32)  NOT NULL,
  PRIMARY KEY (MigrationId)
);

CREATE TABLE AnonymousBallot
(
  AnonymousBallotId bigint           NOT NULL,
  VotingId          int              NOT NULL,
  ChoiceId          int              NOT NULL,
  VoteTag           varbinary(32)    NOT NULL,
  BatchId           uniqueidentifier NULL    ,
  CreatedAt         datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
  PRIMARY KEY (AnonymousBallotId AUTOINCREMENT)
);

CREATE TABLE AspNetRoleClaims
(
  Id         int           NOT NULL,
  RoleId     nvarchar(450) NOT NULL,
  ClaimType  nvarchar(max) NULL    ,
  ClaimValue nvarchar(max) NULL    ,
  PRIMARY KEY (Id AUTOINCREMENT)
);

CREATE TABLE AspNetRoles
(
  Id               nvarchar(450) NOT NULL,
  Name             nvarchar(256) NULL    ,
  NormalizedName   nvarchar(256) NULL     UNIQUE,
  ConcurrencyStamp nvarchar(max) NULL    ,
  PRIMARY KEY (Id),
  FOREIGN KEY (Id) REFERENCES AspNetUserRoles (RoleId),
  FOREIGN KEY (Id) REFERENCES AspNetRoleClaims (RoleId)
);

CREATE TABLE AspNetUserClaims
(
  Id         int           NOT NULL,
  UserId     nvarchar(450) NOT NULL,
  ClaimType  nvarchar(max) NULL    ,
  ClaimValue nvarchar(max) NULL    ,
  PRIMARY KEY (Id AUTOINCREMENT)
);

CREATE TABLE AspNetUserLogins
(
  LoginProvider       nvarchar(128) NOT NULL,
  ProviderKey         nvarchar(128) NOT NULL,
  ProviderDisplayName nvarchar(max) NULL    ,
  UserId              nvarchar(450) NOT NULL,
  PRIMARY KEY (LoginProvider, ProviderKey)
);

CREATE TABLE AspNetUserRoles
(
  UserId nvarchar(450) NOT NULL,
  RoleId nvarchar(450) NOT NULL,
  PRIMARY KEY (UserId, RoleId)
);

CREATE TABLE AspNetUsers
(
  Id                   nvarchar(450)     NOT NULL,
  Name                 nvarchar(256)     NULL    ,
  RefreshToken         nvarchar(1024)    NULL    ,
  DeletedAt            datetime2(0)      NULL    ,
  UserName             nvarchar(256)     NULL    ,
  NormalizedUserName   nvarchar(256)     NULL     UNIQUE,
  Email                nvarchar(256)     NULL    ,
  NormalizedEmail      nvarchar(256)     NULL    ,
  EmailConfirmed       bit               NOT NULL DEFAULT (0),
  PasswordHash         nvarchar(max)     NULL    ,
  SecurityStamp        nvarchar(max)     NULL    ,
  ConcurrencyStamp     nvarchar(max)     NULL    ,
  PhoneNumber          nvarchar(max)     NULL    ,
  PhoneNumberConfirmed bit               NOT NULL DEFAULT (0),
  TwoFactorEnabled     bit               NOT NULL DEFAULT (0),
  LockoutEnd           datetimeoffset(7) NULL    ,
  LockoutEnabled       bit               NOT NULL DEFAULT (0),
  AccessFailedCount    int               NOT NULL DEFAULT (0),
  PRIMARY KEY (Id),
  FOREIGN KEY (Id) REFERENCES AspNetUserRoles (UserId),
  FOREIGN KEY (Id) REFERENCES AspNetUserClaims (UserId),
  FOREIGN KEY (Id) REFERENCES AspNetUserLogins (UserId),
  FOREIGN KEY (Id) REFERENCES AspNetUserTokens (UserId),
  FOREIGN KEY (Id) REFERENCES Votings (CreatedByUserId),
  FOREIGN KEY (Id) REFERENCES VotingParticipation (UserId)
);

CREATE TABLE AspNetUserTokens
(
  UserId        nvarchar(450) NOT NULL,
  LoginProvider nvarchar(128) NOT NULL,
  Name          nvarchar(128) NOT NULL,
  Value         nvarchar(max) NULL    ,
  PRIMARY KEY (UserId, LoginProvider, Name)
);

CREATE TABLE VoteChoices
(
  ChoiceId    int            NOT NULL,
  Name        nvarchar(200)  NOT NULL,
  Description nvarchar(1000) NULL    ,
  VotingId    int            NOT NULL,
  CreatedAt   datetime2(0)   NOT NULL DEFAULT SYSUTCDATETIME(),
  VoteCount   int            NOT NULL DEFAULT (0),
  PRIMARY KEY (ChoiceId AUTOINCREMENT),
  FOREIGN KEY (ChoiceId) REFERENCES AnonymousBallot (ChoiceId)
);

CREATE TABLE VotingParticipation
(
  VotingId     int              NOT NULL,
  UserId       nvarchar(450)    NOT NULL,
  HasVoted     bit              NOT NULL DEFAULT (1),
  VotedBatchId uniqueidentifier NULL    ,
  CreatedAt    datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
  PRIMARY KEY (VotingId, UserId)
);

CREATE TABLE Votings
(
  VotingId        int           NOT NULL,
  Name            nvarchar(200) NOT NULL,
  CreatedAt       datetime2(0)  NOT NULL DEFAULT SYSUTCDATETIME(),
  StartsAt        datetime2(0)  NOT NULL,
  EndsAt          datetime2(0)  NOT NULL,
  CreatedByUserId nvarchar(450) NOT NULL,
  KeySalt         varbinary(16) NOT NULL DEFAULT CRYPT_GEN_RANDOM(16),
  PRIMARY KEY (VotingId AUTOINCREMENT),
  FOREIGN KEY (VotingId) REFERENCES VoteChoices (VotingId),
  FOREIGN KEY (VotingId) REFERENCES VotingParticipation (VotingId),
  FOREIGN KEY (VotingId) REFERENCES AnonymousBallot (VotingId)
);
