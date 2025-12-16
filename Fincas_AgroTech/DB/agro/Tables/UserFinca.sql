CREATE TABLE [agro].[UserFinca] (
    [UserFincaId]  BIGINT         IDENTITY (1, 1) NOT NULL,
    [AspNetUserId] NVARCHAR (450) NOT NULL,
    [FincaId]      BIGINT         NOT NULL,
    PRIMARY KEY CLUSTERED ([UserFincaId] ASC),
    CONSTRAINT [FK_UserFinca_AspNetUsers] FOREIGN KEY ([AspNetUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]),
    CONSTRAINT [FK_UserFinca_Finca] FOREIGN KEY ([FincaId]) REFERENCES [agro].[Finca] ([FincaId])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_UserFinca_AspNetUser_Finca]
    ON [agro].[UserFinca]([AspNetUserId] ASC, [FincaId] ASC);

