public static class FeatureDefaults
{
    public static readonly Dictionary<string, Dictionary<string, object>> All = new()
    {
        ["I2C"] = new()
        {
            ["I2C1_Enable"] = true,
            ["I2C1_Baud"] = 100000
        },
        ["SPI"] = new()
        {
            ["SPI1_Enable"] = true,
            ["SPI1_Mode"] = "Master"
        },
        ["Timer"] = new()
        {
            ["Timer1_Enable"] = true,
            ["Timer1_Period"] = 1000
        }
        // Add more features as needed
    };
}