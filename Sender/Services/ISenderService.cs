﻿using System.Threading.Tasks;

namespace Sender.Services
{
    public interface ISenderService
    {
        Task<bool> Send();
    }
}