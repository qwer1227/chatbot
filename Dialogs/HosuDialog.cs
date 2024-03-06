using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using GreatWall.Helpers;
using System.Threading;

namespace GreatWall
{
    [Serializable]
    public class HosuDialog : IDialog<string>
    {
        private string strMessage;
        private string strOrder;
        private string strServerUrl = "http://localhost:3984/Images/";
        private int numberOfGuests = 0;

        public async Task StartAsync(IDialogContext context)
        {
            strMessage = null;
            strOrder = "[선택하신 방호수] \n";
            await MessageReceivedAsync(context, null);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            try
            {
                if (result != null)
                {
                    Activity activity = await result as Activity;

                    if (activity != null)
                    {
                        if (int.TryParse(activity.Text.Trim(), out numberOfGuests))
                        {
                            

                            // 호수의 최대 인원을 DB에서 확인하여 초과된 경우 알려줌
                            if (CheckMaximumCapacity(strOrder, numberOfGuests))
                            {
                                await context.PostAsync("최대 인원을 초과하였습니다. 다시 입력해주세요.");
                                context.Wait(MessageReceivedAsync);
                            }
                            else
                            {
                                // MonthDialog 호출 및 인자 전달
                                strMessage = string.Format("방호수는 {0}이며, 예약 인원은 {1}명입니다.", strOrder, numberOfGuests);
                                await context.PostAsync(strMessage);
                                await context.Forward(new MonthDialog(strOrder, numberOfGuests), DialogResumeAfter, activity, CancellationToken.None);
                            }
                        }
                        else
                        {
                            strMessage = string.Format("선택하신 호수는 {0}입니다.", activity.Text);

                            strOrder = activity.Text;
                            await context.PostAsync(strMessage);
                            await context.PostAsync("예약인원을 정해주세요");
                            context.Wait(MessageReceivedAsync);
                        }
                    }
                    else
                    {
                        // activity가 null인 경우에 대한 처리
                        strMessage = "원하시는 호수를 선택해 주세요";
                        await context.PostAsync(strMessage);

                        var message = context.MakeMessage();

                        var actions = new List<CardAction>
                {
                    new CardAction(ActionTypes.ImBack, "101호", value: "101호"),
                    new CardAction(ActionTypes.ImBack, "102호", value: "102호"),
                    new CardAction(ActionTypes.ImBack, "103호", value: "103호"),
                    new CardAction(ActionTypes.ImBack, "201호", value: "201호"),
                    new CardAction(ActionTypes.ImBack, "202호", value: "202호"),
                    new CardAction(ActionTypes.ImBack, "203호", value: "203호"),
                    new CardAction(ActionTypes.ImBack, "301호", value: "301호"),
                    new CardAction(ActionTypes.ImBack, "302호", value: "302호"),
                    new CardAction(ActionTypes.ImBack, "303호", value: "303호")
                };

                        var attachment = new HeroCard
                        {
                            Buttons = actions
                        }.ToAttachment();

                        message.Attachments.Add(attachment);
                        message.AttachmentLayout = "carousel";

                        await context.PostAsync(message);
                        context.Wait(MessageReceivedAsync);
                    }
                }
                else
                {
                    // result가 null인 경우에 대한 처리
                    strMessage = "원하시는 호수를 선택해 주세요 ";
                    await context.PostAsync(strMessage);

                    var message = context.MakeMessage();

                    var actions = new List<CardAction>
            {
                new CardAction(ActionTypes.ImBack, "101호", value: "101호"),
                        new CardAction(ActionTypes.ImBack, "102호", value: "102호"),
                        new CardAction(ActionTypes.ImBack, "103호", value: "103호"),
                        new CardAction(ActionTypes.ImBack, "201호", value: "201호"),
                        new CardAction(ActionTypes.ImBack, "202호", value: "202호"),
                        new CardAction(ActionTypes.ImBack, "203호", value: "203호"),
                        new CardAction(ActionTypes.ImBack, "301호", value: "301호"),
                        new CardAction(ActionTypes.ImBack, "302호", value: "302호"),
                        new CardAction(ActionTypes.ImBack, "303호", value: "303호")
            };

                    var attachment = new HeroCard
                    {
                        Buttons = actions
                    }.ToAttachment();

                    message.Attachments.Add(attachment);
                    message.AttachmentLayout = "carousel";

                    await context.PostAsync(message);
                    context.Wait(MessageReceivedAsync);
                }
            }
            catch (Exception ex)
            {
                // 예외 처리
                await context.PostAsync("오류가 발생했습니다. 다시 시도해주세요.");
                context.Wait(MessageReceivedAsync);
            }
        }


        private bool CheckMaximumCapacity(string roomNumber, int numberOfGuests)
        {
            // DB에서 room 테이블을 조회하여 선택된 호수의 최대 인원을 확인
            string connectionString = "Server=localhost; Port=3306; Database=rest; Uid=root; Pwd=rootpw";
            string query = "SELECT max FROM room WHERE name = @name";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@name", roomNumber);

                int maxCapacity = 0;
                object result = command.ExecuteScalar();
                if (result != DBNull.Value)
                {
                    maxCapacity = Convert.ToInt32(result);
                }

                if (numberOfGuests > maxCapacity)
                {
                    return true; // 최대 인원 초과
                }
                else
                {
                    return false; // 최대 인원 이하
                }
            }
        }

        public async Task DialogResumeAfter(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                
            }
            catch (TooManyAttemptsException)
            {
                await context.PostAsync("오류가 발생했습니다.");
            }
        }
    }
}
