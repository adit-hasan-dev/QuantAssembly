using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Collections.Generic;
using QuantAssembly.Strategy;

namespace QuantAssembly.Tests
{
    [TestClass]
    public class StrategyTests
    {
        [TestMethod]
        public void Test_ParseStrategy_WithBothComparators()
        {
            // Arrange
            string jsonContent = File.ReadAllText("TestStrategy.json");


            // Act
            var strategy = JsonConvert.DeserializeObject<Strategy.Strategy>(jsonContent);

            // Assert
            Assert.IsNotNull(strategy);

            // EntryConditions
            Assert.AreEqual(LogicalOperator.AND, strategy.EntryConditions.LogicalOperator);
            Assert.AreEqual(3, strategy.EntryConditions.Conditions.Count);
            Assert.IsInstanceOfType(strategy.EntryConditions.Conditions[0], typeof(PropertyToValueComparator));
            Assert.IsInstanceOfType(strategy.EntryConditions.Conditions[1], typeof(PropertyToValueComparator));
            Assert.IsInstanceOfType(strategy.EntryConditions.Conditions[2], typeof(PropertyToPropertyComparator));

            var entryCondition1 = (PropertyToValueComparator)strategy.EntryConditions.Conditions[0];
            Assert.AreEqual(StrategyProperty.LatestPrice, entryCondition1.Property);
            Assert.AreEqual(StrategyOperator.LessThan, entryCondition1.Operator);
            Assert.AreEqual(50, entryCondition1.Value);

            var entryCondition2 = (PropertyToValueComparator)strategy.EntryConditions.Conditions[1];
            Assert.AreEqual(StrategyProperty.RSI, entryCondition2.Property);
            Assert.AreEqual(StrategyOperator.GreaterThan, entryCondition2.Operator);
            Assert.AreEqual(30, entryCondition2.Value);

            var entryCondition3 = (PropertyToPropertyComparator)strategy.EntryConditions.Conditions[2];
            Assert.AreEqual(StrategyProperty.BidPrice, entryCondition3.LeftHandOperand);
            Assert.AreEqual(StrategyOperator.GreaterThanOrEqual, entryCondition3.Operator);
            Assert.AreEqual(StrategyProperty.AskPrice, entryCondition3.RightHandOperand);

            // ExitConditions
            Assert.AreEqual(LogicalOperator.OR, strategy.ExitConditions.LogicalOperator);
            Assert.AreEqual(3, strategy.ExitConditions.Conditions.Count);
            Assert.IsInstanceOfType(strategy.ExitConditions.Conditions[0], typeof(PropertyToValueComparator));
            Assert.IsInstanceOfType(strategy.ExitConditions.Conditions[1], typeof(PropertyToValueComparator));
            Assert.IsInstanceOfType(strategy.ExitConditions.Conditions[2], typeof(PropertyToPropertyComparator));

            var exitCondition1 = (PropertyToValueComparator)strategy.ExitConditions.Conditions[0];
            Assert.AreEqual(StrategyProperty.RSI, exitCondition1.Property);
            Assert.AreEqual(StrategyOperator.LessThan, exitCondition1.Operator);
            Assert.AreEqual(30, exitCondition1.Value);

            var exitCondition2 = (PropertyToValueComparator)strategy.ExitConditions.Conditions[1];
            Assert.AreEqual(StrategyProperty.SMA, exitCondition2.Property);
            Assert.AreEqual(StrategyOperator.EqualTo, exitCondition2.Operator);
            Assert.AreEqual(20, exitCondition2.Value);

            var exitCondition3 = (PropertyToPropertyComparator)strategy.ExitConditions.Conditions[2];
            Assert.AreEqual(StrategyProperty.Volume, exitCondition3.LeftHandOperand);
            Assert.AreEqual(StrategyOperator.LessThan, exitCondition3.Operator);
            Assert.AreEqual(StrategyProperty.SMA, exitCondition3.RightHandOperand);

            // StopLossConditions and TakeProfitConditions
            Assert.AreEqual(LogicalOperator.AND, strategy.StopLossConditions.LogicalOperator);
            Assert.AreEqual(0, strategy.StopLossConditions.Conditions.Count);
            Assert.AreEqual(LogicalOperator.AND, strategy.TakeProfitConditions.LogicalOperator);
            Assert.AreEqual(0, strategy.TakeProfitConditions.Conditions.Count);
        }
    }
}
