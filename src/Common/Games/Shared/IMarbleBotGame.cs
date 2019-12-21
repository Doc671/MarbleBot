using System;
using System.Threading.Tasks;

namespace MarbleBot.Common
{
    public interface IMarbleBotGame : IDisposable
    {
        Task? Actions { get; set; }
    }
}
