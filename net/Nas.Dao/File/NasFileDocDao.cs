using Com.Scm.Nas.File;
using SqlSugar;

namespace Com.Scm.Nas.Res
{
    /// <summary>
    /// 文档
    /// </summary>
    [SugarTable("nas_res_file")]
    public class NasFileDocDao : NasResFileDao
    {
        public NasFileDocDao()
        {
            type = NasTypeEnums.Doc;
        }
    }
}