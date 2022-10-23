using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RGActionPatches
{
    class Captions
    {
        internal class Actions
        {
            internal static string TalkToSomeone { get { return "人と話す"; } }
            internal static string SpeakWith { get { return "に声をかける"; } }
            internal static string GoToPoleDanceArea { get { return "ポールダンス台に行く"; } }
            internal static string GoToPoleDanceFront { get { return "ポールダンス前に行く"; } }
            internal static string PhysicalCheckup { get { return "体調確認"; } }
            internal static string CheckTemperature { get { return "熱を測る"; } }
            internal static string TalkToPatient { get { return "話しかける"; } }
            internal static string Seduce { get { return "誘惑する"; } }
            internal static string GoToExamChair { get { return "診察椅子に移動"; } }
        }

        internal class Disabled
        {
            internal static string InTheToilet { get { return "トイレ中"; } }
            internal static string TalkingToSomeone { get { return "会話中"; } }
            internal static string InExamination { get { return "診察中"; } }
            internal static string Unavailable { get { return "利用できない"; } }
        }
    }
}
