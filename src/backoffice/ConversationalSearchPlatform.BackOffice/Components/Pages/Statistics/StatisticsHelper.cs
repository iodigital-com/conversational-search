using ApexCharts;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;

namespace ConversationalSearchPlatform.BackOffice.Components.Pages.Statistics;

public static class StatisticsHelper
{
    public static async Task<string?> InitializeTenantName(string? tenantId, IMultiTenantStore<ApplicationTenantInfo> tenantStore)
    {
        if (tenantId != null)
        {
            var tenant = await tenantStore.TryGetAsync(tenantId);
            return tenant?.Name;
        }

        return default;
    }

    public static ApexChartOptions<T> CreateBarChartOptions<T>() where T : class
    {
        return new ApexChartOptions<T>
        {
            Theme = new Theme
            {
                Mode = Mode.Dark,
                Palette = PaletteType.Palette1
            },
            PlotOptions = new PlotOptions
            {
                Bar = new PlotOptionsBar
                {
                    DataLabels = new PlotOptionsBarDataLabels
                    {
                        Total = new BarTotalDataLabels
                        {
                            Formatter = MoneyFormatter(),
                            Enabled = true,
                            Style = new BarDataLabelsStyle
                            {
                                FontWeight = "800"
                            }
                        }
                    }
                }
            }
        };
    }

    private static string MoneyFormatter() =>
        """
        function (value) {
                    if (value === undefined) {return '';}
                        return value + 'EUR';
                }
        """;
}