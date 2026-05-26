using Com.Scm.Dao.User;

namespace Com.Scm.Nas.Res
{
    public class NasResSmsDao : ScmUserDataDao
    {
        public string address { get; set; }
        public string body { get; set; }
        public long date { get; set; }
        public int type { get; set; }
    }
}
