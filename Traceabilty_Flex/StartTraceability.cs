using schemas.xmlsoap.org.ws._2004._08.eventing;
using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Media;
using TraceabilityTestGui;

namespace Traceabilty_Flex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Identifier m_id;


        private bool StartTraceability()
        {
            #region SIPLACE_OIB_SAMPLE_HOST_CALLBACK

            // Build the serive endpoint for the callback interfaces
            // Attention: this can be called only once at one computer.
            try
            {
                if (m_SiplaceTraceabilityNotifyReceiver == null)
                {
                    Uri baseAddress = new Uri(baseAddressTraceability);

                    TraceabilityOibServiceReceiver receiver = new TraceabilityOibServiceReceiver(this);
                    m_SiplaceTraceabilityNotifyReceiver = new ServiceHost(receiver, baseAddress);
                    m_SiplaceTraceabilityNotifyReceiver.Open();
                }
            }
            catch (Exception ex)
            {
                ErrorOut("During initiating the callback service endpoint (Traceability)" + ex);
            }

            #endregion // SIPLACE_OIB_SAMPLE_HOST_CALLBACK

            #region SIPLACE_OIB_SAMPLE_SUBSCRIBE

            try
            {
                using (SubscriptionManagerClient manager = new SubscriptionManagerClient())
                {
                    // Setting the Deliverymode
                    // Here set it to the Push with Acknowledge
                    Subscribe subscribe = new Subscribe
                    {
                        Delivery = new Delivery
                        {
                            DeliveryMode =
                                "http://schemas.xmlsoap.org/ws/2004/08/eventing/DeliveryModes/PushWithAck"
                        }

                    };



                    if (m_SiplaceTraceabilityNotifyReceiver == null)
                        throw new ApplicationException(
                            "Failed to subscribe for setup center events since no notify receiver endpoint was not initialized");
                    if (m_SiplaceTraceabilityNotifyReceiver.ChannelDispatchers.Count < 1)
                        throw new ApplicationException(
                            "Failed to subscribe for setup center events since notify receiver endpoint was not correctly initialized");
                    EndpointAddress notifyTo =
                        new EndpointAddress(m_SiplaceTraceabilityNotifyReceiver.ChannelDispatchers[0].Listener.Uri);
                    subscribe.Delivery.NotifyTo = EndpointAddressAugust2004.FromEndpointAddress(notifyTo);

                    // lifetime of the subscription
                    // Here: Forever
                    Expires exp = new Expires { Value = DateTime.MaxValue.ToUniversalTime() };
                    subscribe.Expires = exp;

                    // See if we have a subscription already for this endpoint 
                    // (use case: client crashed and did not remove subscription)
                    SubscriptionDescriptor desc = new SubscriptionDescriptor()
                    {
                        Endpoint = EndpointAddressAugust2004.FromEndpointAddress(notifyTo),
                        Topic = TraceabilityNotifyTopic
                    };

                    Subscription[] foundSubscriptions = manager.GetSubscriptions(desc);
                    if (foundSubscriptions.Length > 0)
                    {
                        Subscription existing = foundSubscriptions[0];
                        m_id = existing.Manager.Identifier;
                        Renew renew = new Renew()
                        {
                            Expires = new Expires()
                        };
                        renew.Expires.Value = DateTime.Now.AddDays(365).ToUniversalTime();
                        manager.Renew(m_id, TraceabilityNotifyTopic, renew);
                    }
                    else
                    {
                        // action - Subscribe to an topic here the Setup Center events.
                        SubscribeResult subscribeResult = manager.Subscribe("", TraceabilityNotifyTopic, subscribe);

                        // remember the subscription identifier
                        m_id = subscribeResult.SubscriptionManager.Identifier;
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

            #endregion // SIPLACE_OIB_SAMPLE_SUBSCRIBE
            return true;
        }

        public void UnsubscribeTraceability()
        {
            #region SIPLACE_OIB_SAMPLE_UNSUBSCRIBE

            try
            {
                using (SubscriptionManagerClient manager = new SubscriptionManagerClient())
                {
                    Unsubscribe unsubscribe = new Unsubscribe();
                    // action - unsubscribe from 

                    if (m_SiplaceTraceabilityNotifyReceiver != null &&
                        m_SiplaceTraceabilityNotifyReceiver.ChannelDispatchers.Count > 0)
                    {
                        EndpointAddress notifyTo =
                            new EndpointAddress(m_SiplaceTraceabilityNotifyReceiver.ChannelDispatchers[0].Listener.Uri);
                        manager.Unsubscribe(EndpointAddressAugust2004.FromEndpointAddress(notifyTo),
                                            m_id, TraceabilityNotifyTopic, unsubscribe);

                        //MessageBox.Show("Traceability service unsubscribed successfully.", "Unsubscribed", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
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

            #endregion // SIPLACE_OIB_SAMPLE_UNSUBSCRIBE
        }
    }
}