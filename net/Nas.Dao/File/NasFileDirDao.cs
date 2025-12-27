using Com.Scm.Nas.File;
using SqlSugar;

namespace Com.Scm.Nas.Res
{
    /// <summary>
    /// 目录
    /// </summary>
    [SugarTable("nas_res_file")]
    public class NasFileDirDao : NasResFileDao
    {
        public NasFileDirDao()
        {
            type = NasTypeEnums.Dir;
        }
    }
}