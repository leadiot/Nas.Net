using SqlSugar;

namespace Com.Scm.Nas.Res
{
    /// <summary>
    /// 目录
    /// </summary>
    [SugarTable("nas_res_file")]
    public class NasResFileDirDao : NasResFileDao
    {
        public NasResFileDirDao()
        {
            type = NasTypeEnums.Dir;
        }
    }
}