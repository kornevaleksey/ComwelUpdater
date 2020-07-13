using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ConfigUpdater
{
    public interface IConfigUpdater
    {
        Dictionary<string, string> Config { get; }
        string FileName { get; set; }
        
        Task<bool> ReadAsync();
        Task<bool> WriteAsync();
        void SetDefault();
    }
}
