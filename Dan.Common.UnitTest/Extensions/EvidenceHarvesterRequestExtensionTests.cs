using Dan.Common.Extensions;
using FluentAssertions;

namespace Dan.Common.UnitTest.Extensions;

[TestClass]
public class EvidenceHarvesterRequestExtensionTests
{
    [TestMethod]
    [DataRow(1, true, 1)]
    [DataRow("1", true, 1)]
    [DataRow(null, false, 0)]
    [DataRow(3000000000, false, 0)]
    [DataRow(-3000000000, false, 0)]
    public void TryGetParameter_Numbers(object value, bool expectedBool, int expectedInt)
    {
        // Arrange
        const string parameterName = "NumberParam";
        var request = new EvidenceHarvesterRequest
        {
            Parameters =
            [
                new EvidenceParameter()
                {
                    EvidenceParamName = parameterName,
                    Value = value
                }
            ]
        };
        
        // Act
        var actualBool = request.TryGetParameter(parameterName, out int actualInt);

        // Assert
        actualBool.Should().Be(expectedBool);
        actualInt.Should().Be(expectedInt);
    }
    
    [TestMethod]
    [DataRow(1, true, 1)]
    [DataRow(1.2, true, 1.2)]
    [DataRow("1", true, 1)]
    [DataRow("1.2", true, 1.2)]
    [DataRow("1,2", true, 1.2)]
    [DataRow("1200", true, 1200)]
    [DataRow("1200.00", true, 1200)]
    [DataRow("1200,00", true, 1200)]
    [DataRow("1,200.00", true, 1200)]
    [DataRow("1 200,00", true, 1200)]
    [DataRow("1,200", true, 1200)]
    [DataRow("1 200", true, 1200)]
    [DataRow("1,200,000", true, 1200000)]
    [DataRow("1 200 000", true, 1200000)]
    [DataRow("1,200,000.00", true, 1200000)]
    [DataRow("1 200 000,00", true, 1200000)]
    [DataRow(null, false, 0)]
    [DataRow(3000000000d, true, 3000000000)]
    [DataRow(-3000000000d, true, -3000000000)]
    public void TryGetParameter_Decimal(object value, bool expectedBool, double expectedTemp)
    {
        // Arrange
        // decimal is not const so need to cast
        var expectedDecimal = (decimal)expectedTemp;
        const string parameterName = "NumberParam";
        var request = new EvidenceHarvesterRequest
        {
            Parameters =
            [
                new EvidenceParameter()
                {
                    EvidenceParamName = parameterName,
                    Value = value
                }
            ]
        };
        
        // Act
        var actualBool = request.TryGetParameter(parameterName, out decimal actualDecimal);

        // Assert
        actualBool.Should().Be(expectedBool);
        actualDecimal.Should().Be(expectedDecimal);
    }
}