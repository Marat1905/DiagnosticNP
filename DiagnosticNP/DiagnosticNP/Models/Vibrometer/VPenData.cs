using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiagnosticNP.Models.Vibrometer
{
    public class VPenData
    {
        public stVPenData Header { get; set; }

        public async Task<float[]> CheckAndConvert()
        {
            return await Task.Run(() =>
            {
                var timestamp = Header.Header.Timestamp;
                var factor = Header.Header.Coeff;
                var items = new List<short>();

                foreach (var i in Header.Blocks.OrderBy(j => j.ViPen_Get_Data_Block))
                {
                    if (i.ViPen_Get_Wave_ID != Header.Header.ViPen_Get_Wave_ID)
                        throw new Exception("Incorrect timestamp!");

                    items.AddRange(i.Data);
                }

                var result = new float[items.Count];

                for (int i = 0; i < Math.Min(1600, result.Length); i++)
                {
                    result[i] = items[i] * factor;
                }

                return result;
            });
        }
    }
}