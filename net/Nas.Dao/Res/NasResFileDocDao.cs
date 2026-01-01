using SqlSugar;

namespace Com.Scm.Nas.Res
{
    /// <summary>
    /// 文档
    /// </summary>
    [SugarTable("nas_res_file")]
    public class NasResFileDocDao : NasResFileDao
    {
        public NasResFileDocDao()
        {
            type = NasTypeEnums.Doc;
        }
    }
}