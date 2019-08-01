﻿using System;
using Sessions.Models;

namespace SessionsVoting.API.Repositories.Contracts
{
    public interface ISessionsRepository
    {
        Session GetSessionById(Guid sessionId);
    }
}