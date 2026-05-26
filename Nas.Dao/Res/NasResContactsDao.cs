using Com.Scm.Dao.User;

namespace Com.Scm.Nas.Res
{
    public class NasResContactsDao : ScmUserDataDao
    {
        public string name { get; set; }
        public List<string> phones { get; set; }
        public List<string> emails { get; set; }
    }
}
