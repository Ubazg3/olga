-- Checkers server database schema. Access SQL dialect.
-- Tables: Users, Games, Moves. Moves is the link table — every move
-- belongs to a Game, and reaches both players via Games.WhitePlayerId
-- / BlackPlayerId.

CREATE TABLE Users (
    ID             COUNTER PRIMARY KEY,
    Username       TEXT(50)     NOT NULL,
    PasswordHash   TEXT(128)    NOT NULL,
    Email          TEXT(120),
    [Role]         INTEGER      NOT NULL,
    Wins           INTEGER      NOT NULL,
    Losses         INTEGER      NOT NULL,
    Draws          INTEGER      NOT NULL,
    Rating         INTEGER      NOT NULL,
    CreatedAt      DATETIME     NOT NULL,
    IsBanned       BIT          NOT NULL,
    ProfilePicture LONGBINARY,
    BirthDate      DATETIME,
    Country        TEXT(50)
);

CREATE UNIQUE INDEX IX_Users_Username ON Users (Username);

CREATE TABLE Games (
    ID            COUNTER PRIMARY KEY,
    WhitePlayerId INTEGER      NOT NULL REFERENCES Users(ID),
    BlackPlayerId INTEGER      NOT NULL REFERENCES Users(ID),
    StartedAt     DATETIME     NOT NULL,
    EndedAt       DATETIME,
    Status        INTEGER      NOT NULL,
    EndReason     INTEGER      NOT NULL,
    WinnerId      INTEGER,
    FinalBoard    MEMO,
    MoveCount     INTEGER      NOT NULL
);

CREATE TABLE Moves (
    ID             COUNTER PRIMARY KEY,
    GameId         INTEGER     NOT NULL REFERENCES Games(ID),
    MoveNumber     INTEGER     NOT NULL,
    MoverColor     INTEGER     NOT NULL,
    FromFile       INTEGER     NOT NULL,
    FromRank       INTEGER     NOT NULL,
    ToFile         INTEGER     NOT NULL,
    ToRank         INTEGER     NOT NULL,
    PathSerialized TEXT(120),
    Piece          INTEGER     NOT NULL,
    IsCapture      BIT         NOT NULL,
    CapturedCount  INTEGER     NOT NULL,
    BecameKing     BIT         NOT NULL,
    Notation       TEXT(40),
    BoardAfter     MEMO,
    PlayedAt       DATETIME    NOT NULL
);

CREATE INDEX IX_Moves_Game ON Moves (GameId);
