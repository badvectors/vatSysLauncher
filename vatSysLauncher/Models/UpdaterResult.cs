using vatSysLauncher.Controllers;

namespace vatSysLauncher.Models
{
    public class UpdaterResult
    {
        public bool Success { get; set; } = false;
        public List<string> Log { get; private set; } = [];

        public void Add(string log)
        {
            Log.Add(log);

            Updater.SetLog(log);
        }
    }
}
