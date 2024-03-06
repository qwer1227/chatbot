using System;
using System.Threading.Tasks;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Collections.Generic;       //Add for List<>

namespace GreatWall
{
    [Serializable]
    public class RootDialog : IDialog<string>
    {
        protected int count = 1;
        string strMessage;
        private string strWelcomeMessage = "[Rent Room service Bot]";

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            await context.PostAsync(strWelcomeMessage);    //return our reply to the user

            var message = context.MakeMessage();        //Create message
            var actions = new List<CardAction>();       //Create List

            actions.Add(new CardAction() { Title = "�����ϱ�", Value = "�����ϱ�", Type = ActionTypes.ImBack });
            actions.Add(new CardAction() { Title = "�������� Ȯ���ϱ�", Value = "�������� Ȯ���ϱ�", Type = ActionTypes.ImBack });

            message.Attachments.Add(                    //Create Hero Card & attachment
                new HeroCard { Title = "���Ͻô� �޴��� ������ �ּ���", Buttons = actions }.ToAttachment()
            );

            await context.PostAsync(message);           //return our reply to the user

            context.Wait(SendWelcomeMessageAsync);
        }

        public async Task SendWelcomeMessageAsync(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;
            string strSelected = activity.Text.Replace(" ", "");

            if (strSelected == "�����ϱ�")
            {
                context.Call(new OrderDialog(), DialogResumeAfter);
            }
            else if (strSelected == "��������Ȯ���ϱ�")
            {
                context.Call(new CHKDialog(), DialogResumeAfter);
            }
            else
            {
                strMessage = "�߸� �����ϼ̽��ϴ�. �ٽ� ������ �ּ���.";
                await context.PostAsync(strMessage);
                context.Wait(SendWelcomeMessageAsync);
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

        public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                this.count = 1;
                await context.PostAsync("Reset count.");
            }
            else
            {
                await context.PostAsync("Did not reset count.");
            }
            context.Wait(MessageReceivedAsync);
        }
    }
}

