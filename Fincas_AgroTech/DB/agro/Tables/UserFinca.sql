CREATE TABLE [agro].[UserFinca] (
    [UserId]  BIGINT NOT NULL,
    [FincaId] BIGINT NOT NULL,
    PRIMARY KEY CLUSTERED ([UserId] ASC, [FincaId] ASC),
    FOREIGN KEY ([FincaId]) REFERENCES [agro].[Finca] ([FincaId]),
    FOREIGN KEY ([UserId]) REFERENCES [agro].[User] ([UserId])
);

