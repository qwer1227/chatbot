using System;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;


namespace GreatWall
{
    [Serializable]
    public class MonthDialog : IDialog<string>
    {

        private string strWelcomeMessage = "[월을 정해주세요] ex) 잘쳐줘";
        private string strOrder;
        private int numberOfGuests;

        public MonthDialog(string order, int numberOfGuests)
        {
            this.numberOfGuests = numberOfGuests;
            strOrder = order;
        }


        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            await context.PostAsync(strWelcomeMessage);
            await DisplayMonthCardsAsync(context);
        }

        public async Task DisplayMonthCardsAsync(IDialogContext context)
        {
            var message = context.MakeMessage();

            var actions = new List<CardAction>();
            for (int i = 1; i <= 12; i++)
            {
                var action = new CardAction
                {
                    Title = $"{i}월",
                    Type = ActionTypes.ImBack,
                    Value = i.ToString()
                };
                actions.Add(action);
            }

            var heroCard = new HeroCard
            {
                Title = "월을 선택해주세요.",
                Buttons = actions
            };

            message.Attachments = new List<Attachment> { heroCard.ToAttachment() };

            await context.PostAsync(message);
            context.Wait(ProcessSelectedMonthAsync);
        }

        public async Task ProcessSelectedMonthAsync(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;
            string strSelected = activity.Text.Trim();

            int selectedMonth;
            if (int.TryParse(strSelected, out selectedMonth) && selectedMonth >= 1 && selectedMonth <= 12)
            {
                await context.PostAsync($"{selectedMonth}월로 안내하겠습니다");

                

                // Move to DayDialog and pass selectedMonth
                context.Call(new DayDialog(strOrder, selectedMonth,numberOfGuests), DialogResumeAfter);
            }
            else
            {
                await context.PostAsync("You have made a mistake. Please select again...");
                await DisplayMonthCardsAsync(context);
            }
        }




        public async Task DialogResumeAfter(IDialogContext context, IAwaitable<object> result)
        {
            try
            {
                await result;

                context.Done(string.Empty);
            }
            catch (TooManyAttemptsException)
            {
                await context.PostAsync("Error occurred....");
            }
        }
    }
}
