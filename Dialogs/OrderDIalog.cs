using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Threading.Tasks;           //Add to process Async Task
using Microsoft.Bot.Connector;          //Add for Activity Class
using Microsoft.Bot.Builder.Dialogs;    //Add for Dialog Class
using System.Net.Http;                  //Add for internet
using GreatWall.Helpers;                //Add for CardHelper

namespace GreatWall
{
    [Serializable]
    public class OrderDialog : IDialog<string>
    {
        string strMessage;
        string strOrder;
        string strServerUrl = "http://localhost:3984/Images/";

        public async Task StartAsync(IDialogContext context)
        {
            strMessage = null;
            strOrder = "[��? ���� ��? ����] \n";

            //Called MessageReceivedAsync() without user input message
            await this.MessageReceivedAsync(context, null);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            await context.PostAsync(strOrder);    //return our reply to the user

            var message = context.MakeMessage();        //Create message
            var actions = new List<CardAction>();       //Create List

            actions.Add(new CardAction() { Title = "�� ���� ����", Value = "�� ���� ����", Type = ActionTypes.ImBack });
            actions.Add(new CardAction() { Title = "��¥ ���� ����", Value = "��¥ ���� ����", Type = ActionTypes.ImBack });

            message.Attachments.Add(                    //Create Hero Card & attachment
                new HeroCard { Title = "���Ͻô� �޴��� ������ �ּ���", Buttons = actions }.ToAttachment()
            );

            await context.PostAsync(message);           //return our reply to the user

            context.Wait(SendRentAsync);
        }
        public async Task SendRentAsync(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;
            string strSelected = activity.Text.Replace(" ", "");

            if (strSelected == "���������")
            {
                context.Call(new HosuDialog(), DialogResumeAfter);
            }
            else if (strSelected == "��¥��������")
            {
                context.Call(new RentDialog(), DialogResumeAfter);
            }
            else
            {
                strMessage = "�߸� �����ϼ̽��ϴ�. �ٽ� ������ �ּ���.";
                await context.PostAsync(strMessage);
                context.Wait(SendRentAsync);
            }
        }

        public async Task DialogResumeAfter(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                strMessage = await result;

                //await context.PostAsync(WelcomeMessage); ;
                await this.MessageReceivedAsync(context, result);
            }
            catch (TooManyAttemptsException)
            {
                await context.PostAsync("Error occurred....");
            }
        }
    }

}