using AdsSyncClientDemo.Models;
using AdsSync;
using TwinCAT.Ads;
using UnitTest.Mocks;

namespace UnitTest
{
    public class AdsSyncClientTest
    {
        [Fact]
        public async Task ConnectAsync()
        {
            AdsSyncClient client = null;
            try
            {
                //Arrange
                IAdsConnectAddress adsClient = new MockAdsClient();
                AdsDataObject dataToPlc = new();
                AdsDataObject dataFromPlc = new();
                AmsAddress amsAddress = new("127.0.0.1.1.1", 851);
                client = new(adsClient, amsAddress, dataToPlc, dataFromPlc, "MAIN.DataFromHmi", "Main.DataToHmi");
                //Act
                bool connected = await client.ActivateSync();
                //Assert
                Assert.True(connected);
            }
            catch
            {
                Assert.Fail();
            }
            finally
            {
                if (client is not null)
                {
                    await client.DisposeAsync();
                }
            }
        }

        [Fact]
        public async Task DisconnectAsync()
        {
            AdsSyncClient client = null;
            try
            {
                //Arrange
                IAdsConnectAddress adsClient = new MockAdsClient();
                AdsDataObject dataToPlc = new();
                AdsDataObject dataFromPlc = new();
                AmsAddress amsAddress = new("127.0.0.1.1.1", 851);
                client = new(adsClient, amsAddress, dataToPlc, dataFromPlc, "MAIN.DataFromHmi", "Main.DataToHmi");
                //Act
                bool connected = await client.ActivateSync();
                bool disconnected = connected
                    ? await client.StopSyncAsync()
                    : false;
                //Assert
                Assert.True(connected && disconnected);
            }
            catch
            {
                Assert.Fail();
            }
            finally
            {
                if (client is not null)
                {
                    await client.DisposeAsync();
                }
            }
        }

        public async Task
    }
}