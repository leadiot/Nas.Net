using Com.Scm.Dsa;
using Com.Scm.Token;
using SqlSugar;

namespace Com.Scm.Nas.Res
{
    public class NasResAppsService : NasResFileService
    {
        public NasResAppsService(SugarRepository<NasResFileDao> thisRepository, ISqlSugarClient sqlClient, IJwtTokenHolder jwtHolder, IResHolder resHolder)
            : base(thisRepository, sqlClient, jwtHolder, resHolder)
        {
        }

        protected override long GetRootDirId()
        {
            var path = NasEnv.PathApps;
            var dao = _thisRepository.AsQueryable()
                .Where(a => a.path == path)
                .First();

            return dao != null ? dao.id : 0;
        }
    }
}
