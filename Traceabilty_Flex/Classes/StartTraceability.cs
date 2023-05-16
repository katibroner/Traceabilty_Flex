using schemas.xmlsoap.org.ws._2004._08.eventing;
using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Media;


namespace Traceabilty_Flex
{
    public partial class MainWindow : Window
    {
        private static Identifier _mId;

        private bool StartTraceability()
        {
            // Build the service endpoint for the callback interfaces - Attention: this can be called only once at one computer.
            try
            {
                if (TraceabilityNotifyReceiver == null)
                {
                    var baseAddress = new Uri(baseAddressTraceability);

                    var receiver = new TraceabilityOibServiceReceiver(this);
                    TraceabilityNotifyReceiver = new ServiceHost(receiver, baseAddress);
                    TraceabilityNotifyReceiver.Open();
                }
            }
            catch (Exception ex)
            {
                ErrorOut("During initiating the callback service endpoint (Traceability)" + ex);
            }

            try
            {
                using (var manager = new SubscriptionManagerClient())
                {
                    // Setting the Delivery mode - Here set it to the Push with Acknowledge
                    var subscribe = new Subscribe
                    {
                        Delivery = new Delivery
                        {
                            DeliveryMode = "http://schemas.xmlsoap.org/ws/2004/08/eventing/DeliveryModes/PushWithAck"
                        }
                    };

                    if (TraceabilityNotifyReceiver == null)
                        throw new ApplicationException("Failed to subscribe for setup center events since no notify receiver endpoint was not initialized");

                    if (TraceabilityNotifyReceiver.ChannelDispatchers.Count < 1)
                        throw new ApplicationException("Failed to subscribe for setup center events since notify receiver endpoint was not correctly initialized");

                    var channelListener = TraceabilityNotifyReceiver.ChannelDispatchers[0].Listener;
                    if (channelListener != null)
                    {
                        var notifyTo = new EndpointAddress(channelListener.Uri);
                        subscribe.Delivery.NotifyTo = EndpointAddressAugust2004.FromEndpointAddress(notifyTo);

                        // lifetime of the subscription - Here: Forever
                        var exp = new Expires { Value = DateTime.MaxValue.ToUniversalTime() };
                        subscribe.Expires = exp;

                        // See if we have a subscription already for this endpoint (use case: client crashed and did not remove subscription)
                        var desc = new SubscriptionDescriptor()
                        {
                            Endpoint = EndpointAddressAugust2004.FromEndpointAddress(notifyTo),
                            Topic = TraceabilityNotifyTopic
                        };

                        var foundSubscriptions = manager.GetSubscriptions(desc);
                        if (foundSubscriptions.Length > 0)
                        {
                            var existing = foundSubscriptions[0];
                            _mId = existing.Manager.Identifier;
                            var renew = new Renew()
                            {
                                Expires = new Expires()
                            };
                            renew.Expires.Value = DateTime.Now.AddDays(365).ToUniversalTime();
                            manager.Renew(_mId, TraceabilityNotifyTopic, renew);
                        }
                        else
                        {
                            // action - Subscribe to an topic here the Setup Center events.
                            var subscribeResult = manager.Subscribe("", TraceabilityNotifyTopic, subscribe);

                            // remember the subscription identifier
                            _mId = subscribeResult.SubscriptionManager.Identifier;
                        }
                    }
                }
            }

            catch (CommunicationException communicationExcpetion)
            {
                CommunicationExceptionOut("During Subscribe Traceability", communicationExcpetion, true);
            }
            catch (Exception exPing)
            {
                ExceptionOut("During Subscribe Traceability", exPing, true);
            }

            LabelTraceability.Background = Brushes.Green;

            return true;
        }

        private void UnsubscribeTraceability()
        {
            try
            {
                using (var manager = new SubscriptionManagerClient())
                {
                    var unsubscribe = new Unsubscribe();

                    // action - unsubscribe from 
                    if (TraceabilityNotifyReceiver == null || TraceabilityNotifyReceiver.ChannelDispatchers.Count <= 0) return;

                    var channelListener = TraceabilityNotifyReceiver.ChannelDispatchers[0].Listener;
                    if (channelListener == null) return;

                    var notifyTo = new EndpointAddress(channelListener.Uri);
                    manager.Unsubscribe(EndpointAddressAugust2004.FromEndpointAddress(notifyTo),_mId, TraceabilityNotifyTopic, unsubscribe);
                }
            }
            catch (CommunicationException communicationExcpetion)
            {
                CommunicationExceptionOut("During Unsubscribe Traceability", communicationExcpetion, true);
            }
            catch (Exception exPing)
            {
                ExceptionOut("During Unsubscribe Traceability", exPing, true);
            }
        }
    }
}