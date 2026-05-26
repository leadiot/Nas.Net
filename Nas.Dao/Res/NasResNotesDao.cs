using Com.Scm.Dao.User;

namespace Com.Scm.Nas.Res
{
    public class NasResNotesDao : ScmUserDataDao
    {
        public string title { get; set; }
        public string content { get; set; }
        public long dateCreated { get; set; }
        public long dateModified { get; set; }
        public int color { get; set; }
    }
}
