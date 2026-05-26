using Com.Scm.Dao.User;

namespace Com.Scm.Nas.Res
{
    public class NasResCallLogDao : ScmUserDataDao
    {
        public string number { get; set; }
        public string name { get; set; }
        public long date { get; set; }
        public int type { get; set; }
        public long duration { get; set; }
    }
}
