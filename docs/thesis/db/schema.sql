/* ============================================================
   02_up_migration.sql
   Up migration from legacy design:
     - Legacy table: dbo.Votes(UserId, ChoiceId, VotingId, CreatedAt)
     - Existing tables: dbo.Votings, dbo.VoteChoices, dbo.AspNetUsers
   No down migration (as requested).
   Target: Microsoft SQL Server
   ============================================================ */

SET NOCOUNT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    /* 1) Add KeySalt to Votings (non-secret, per voting) */
    IF COL_LENGTH('dbo.Votings', 'KeySalt') IS NULL
    BEGIN
        ALTER TABLE dbo.Votings
        ADD KeySalt varbinary(16) NOT NULL
            CONSTRAINT DF_Votings_KeySalt DEFAULT (CRYPT_GEN_RANDOM(16));
    END

    /* 2) Optional: add cached tally to VoteChoices */
    IF COL_LENGTH('dbo.VoteChoices', 'VoteCount') IS NULL
    BEGIN
        ALTER TABLE dbo.VoteChoices
        ADD VoteCount int NOT NULL
            CONSTRAINT DF_VoteChoices_VoteCount DEFAULT (0);
    END

    /* 3) Create VotingParticipation (if not exists) */
    IF OBJECT_ID('dbo.VotingParticipation', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.VotingParticipation
        (
            VotingId      int NOT NULL,
            UserId        nvarchar(450) NOT NULL,
            HasVoted      bit NOT NULL CONSTRAINT DF_VotingParticipation_HasVoted DEFAULT (1),
            VotedBatchId  uniqueidentifier NULL,
            CreatedAt     datetime2(0) NOT NULL CONSTRAINT DF_VotingParticipation_CreatedAt DEFAULT (SYSUTCDATETIME()),

            CONSTRAINT PK_VotingParticipation PRIMARY KEY (VotingId, UserId),
            CONSTRAINT FK_VotingParticipation_Voting FOREIGN KEY (VotingId) REFERENCES dbo.Votings (VotingId) ON DELETE CASCADE,
            CONSTRAINT FK_VotingParticipation_User   FOREIGN KEY (UserId)   REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
        );

        CREATE INDEX IX_VotingParticipation_VotingId ON dbo.VotingParticipation (VotingId);
    END

    /* 4) Create AnonymousBallot (if not exists) */
    IF OBJECT_ID('dbo.AnonymousBallot', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AnonymousBallot
        (
            AnonymousBallotId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AnonymousBallot PRIMARY KEY,

            VotingId   int NOT NULL,
            ChoiceId   int NOT NULL,
            VoteTag    varbinary(32) NOT NULL,

            BatchId    uniqueidentifier NULL,
            CreatedAt  datetime2(0) NOT NULL CONSTRAINT DF_AnonymousBallot_CreatedAt DEFAULT (SYSUTCDATETIME()),

            CONSTRAINT FK_AnonymousBallot_Voting FOREIGN KEY (VotingId) REFERENCES dbo.Votings (VotingId) ON DELETE CASCADE,
            CONSTRAINT FK_AnonymousBallot_Choice FOREIGN KEY (ChoiceId) REFERENCES dbo.VoteChoices (ChoiceId)
        );

        CREATE UNIQUE INDEX UX_AnonymousBallot_VotingId_VoteTag
        ON dbo.AnonymousBallot (VotingId, VoteTag);

        CREATE INDEX IX_AnonymousBallot_VotingId_ChoiceId
        ON dbo.AnonymousBallot (VotingId, ChoiceId);
    END

    /* 5) Backfill from legacy Votes table (if it exists) */
    IF OBJECT_ID('dbo.Votes', 'U') IS NOT NULL
    BEGIN
        /* 5a) Participation: distinct (VotingId, UserId) */
        INSERT INTO dbo.VotingParticipation (VotingId, UserId, HasVoted, CreatedAt)
        SELECT DISTINCT
            v.VotingId,
            v.UserId,
            CAST(1 AS bit),
            ISNULL(v.CreatedAt, SYSUTCDATETIME())
        FROM dbo.Votes v
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.VotingParticipation p
            WHERE p.VotingId = v.VotingId AND p.UserId = v.UserId
        );

        /* 5b) Anonymous ballots: one row per legacy vote
               NOTE: historical VoteTag is generated randomly for diagram/testing.
               Real system uses HMAC(K_vote, receipt) in the application layer. */
        INSERT INTO dbo.AnonymousBallot (VotingId, ChoiceId, VoteTag, CreatedAt)
        SELECT
            v.VotingId,
            v.ChoiceId,
            HASHBYTES('SHA2_256', CRYPT_GEN_RANDOM(32)) AS VoteTag,
            ISNULL(v.CreatedAt, SYSUTCDATETIME())       AS CreatedAt
        FROM dbo.Votes v;

        /* 5c) Optional: recompute cached counts from legacy votes */
        IF COL_LENGTH('dbo.VoteChoices', 'VoteCount') IS NOT NULL
        BEGIN
            ;WITH c AS
            (
                SELECT v.ChoiceId, COUNT(*) AS Cnt
                FROM dbo.Votes v
                GROUP BY v.ChoiceId
            )
            UPDATE vc
            SET vc.VoteCount = c.Cnt
            FROM dbo.VoteChoices vc
            INNER JOIN c ON c.ChoiceId = vc.ChoiceId;
        END

        /* 5d) Legacy table handling: drop or rename (pick one) */

        -- Option 1 (recommended for anonymity): DROP legacy table
        DROP TABLE dbo.Votes;

        -- Option 2 (if you want to keep it temporarily): rename instead of drop
        -- EXEC sp_rename 'dbo.Votes', 'Votes_Legacy';
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

    DECLARE @Err nvarchar(4000) = ERROR_MESSAGE();
    RAISERROR('Up migration failed: %s', 16, 1, @Err);
END CATCH;
GO