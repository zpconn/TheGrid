using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TheGrid
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );

            Gas.Helpers.Config config = new Gas.Helpers.Config( "Config.txt" );

            using ( TheGridForm form = new TheGridForm() )
            {
                form.Run( config.GetSetting<bool>( "Windowed" ),
                    config.GetSetting<int>( "DesiredWidth" ),
                    config.GetSetting<int>( "DesiredHeight" ),
                    config.GetSetting<string>( "WindowTitle" ) );
            }
        }
    }
}