using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Data;
using MySql.Data.MySqlClient;
using System.Net.Http;
using GreatWall.Helpers;
using System.Data.Common;
using System.Collections.Generic;
using System.Linq;

namespace GreatWall
{
    [Serializable]
    public class DayDialog : IDialog<string>
    {
        private int selectedMonth;
        private string strOrder;
        private int rentDay;
        string strMessage;
        private string startDay;
        private int numberOfGuests;

        public DayDialog(string order, int month, int guests)
        {
            strOrder = order;
            selectedMonth = month;
            numberOfGuests = guests;
        }

        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync("시작일을 선택해주세요:");
            context.Wait(OnStartDaySelectedAsync);
        }

        private async Task OnStartDaySelectedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            startDay = message.Text.Trim();

            // 시작일을 선택한 후, 몇 일을 선택할지 물어보기
            await context.PostAsync($"{startDay}부터 몇 일 동안 예약하시겠습니까?");
            context.Wait(OnRentDayReceivedAsync);
        }

        private async Task OnRentDayReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            string selectedRentDay = message.Text.Trim();

            try
            {
                if (int.TryParse(selectedRentDay, out rentDay))
                {
                    if (rentDay > 14)
                    {
                        await context.PostAsync("예약 가능한 최대 대여 기간은 14일입니다.");
                        context.Wait(OnRentDayReceivedAsync);
                        return;
                    }

                    // 사용자가 선택한 시작일과 예약 기간을 기반으로 예약 가능한 범위를 계산합니다.
                    int startDayValue = int.Parse(startDay);
                    int endDayValue = startDayValue + rentDay - 1;



                    

                    // 데이터베이스에서 해당 호수의 예약된 날짜를 조회합니다.

                    await GetReservedDaysFromDatabase(context,strOrder, selectedMonth, startDayValue, rentDay);
                    // 예약 가능한 범위 내에 이미 예약된 날짜가 있는지 확인합니다.
                    

                    // 이후 코드 생략...

                    // 예약 가능한 범위 안에 있는 경우, 휴대폰 번호를 입력받는 단계로 진행합니다.
                    
                }
                else
                {
                    await context.PostAsync("올바른 숫자를 입력해주세요.");
                    context.Wait(OnRentDayReceivedAsync);
                }
            }
            catch (Exception ex)
            {
                await context.PostAsync("오류가 발생했습니다: " + ex.Message);
            }
        }

        private async Task OnPhoneNumberReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            string phone = message.Text.Trim();

            if (phone.Length == 11) // 휴대폰 번호가 11자리인지 확인
            {
                // 휴대폰 번호를 입력받은 후, 비밀번호를 입력받음
                await context.PostAsync("비밀번호를 입력해주세요:");

                context.PrivateConversationData.SetValue<string>("phone", phone);

                context.Wait(OnPasswordReceivedAsync); // 비밀번호를 입력받는 메서드로 이동
            }
            else
            {
                await context.PostAsync("올바른 휴대폰 번호를 입력해주세요 (11자리, '-' 제외)");
                context.Wait(OnPhoneNumberReceivedAsync);
            }
        }

        private async Task OnPasswordReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            string password = message.Text.Trim();

            // state에서 휴대폰 번호 가져오기
            string phone = context.PrivateConversationData.GetValueOrDefault<string>("phone");
            try
            {
                // DB에 값을 저장
                using (MySqlConnection connection = new MySqlConnection("Server=localhost;Port=3306;Database=rest;Uid=root;Pwd=rootpw"))
                {
                    await connection.OpenAsync();

                    // 방 정보 조회
                    string roomCommand = $"SELECT * FROM room WHERE name = '{strOrder}'";

                    using (MySqlCommand command = new MySqlCommand(roomCommand, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows && reader.Read())
                            {
                                int basePrice = reader.GetInt32("price");
                                int totalGuests = reader.GetInt32("max");
                                int additionalPrice = reader.GetInt32("addprice");
                                reader.Close();
                                // 시작일과 선택한 일 수로 예약 기간 생성
                                int startDayValue = int.Parse(startDay);
                                int endDayValue = startDayValue + rentDay - 1;

                                // 인원당 추가 금액 계산
                                int additionalPricePerPerson = 0;
                                int guests = numberOfGuests; // 사용자가 입력한 인원 수
                                if (guests > totalGuests)
                                {
                                    additionalPricePerPerson = (guests - totalGuests) * additionalPrice;
                                }

                                // 총 가격 계산
                                int total = basePrice + guests * additionalPricePerPerson;
                                total = total * rentDay;
                                int day = int.Parse(startDay);
                                int month = selectedMonth;

                                for (int i = 0; i < rentDay; i++)
                                {
                                    // 선택한 월이 홀수 월인 경우
                                    if (month % 2 == 1)
                                    {
                                        // 31일까지 있는 월이면서 시작일이 31일을 넘어가는 경우
                                        if (month <= 7 && day > 31)
                                        {
                                            month += 1;
                                            day = 1;
                                        }
                                        // 30일까지 있는 월이면서 시작일이 30일을 넘어가는 경우
                                        else if (month > 7 && day > 30)
                                        {
                                            month += 1;
                                            day = 1;
                                        }
                                    }
                                    // 선택한 월이 짝수 월인 경우
                                    else
                                    {
                                        // 30일까지 있는 월이면서 시작일이 30일을 넘어가는 경우
                                        if (month <= 7 && day > 30)
                                        {
                                            month += 1;
                                            day = 1;
                                        }
                                        // 31일까지 있는 월이면서 시작일이 31일을 넘어가는 경우
                                        else if (month > 7 && day > 31)
                                        {
                                            month += 1;
                                            day = 1;
                                        }
                                    }

                                    // 2월인 경우
                                    if (month == 2)
                                    {
                                        // 시작일이 28일을 넘어가는 경우
                                        if (day > 28)
                                        {
                                            month += 1;
                                            day = 1;
                                        }
                                    }

                                    // INSERT 쿼리 실행
                                    string insertQuery = "INSERT INTO reserve (name, month, day, rentday, phone, pwd, total,per) VALUES (@name, @month, @day, @rentday, @phone, @pwd, @total, @per)";
                                    using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection))
                                    {
                                        insertCommand.Parameters.AddWithValue("@name", strOrder);
                                        insertCommand.Parameters.AddWithValue("@month", month);
                                        insertCommand.Parameters.AddWithValue("@day", day);
                                        insertCommand.Parameters.AddWithValue("@rentday", rentDay);
                                        insertCommand.Parameters.AddWithValue("@phone", phone);
                                        insertCommand.Parameters.AddWithValue("@pwd", password);
                                        insertCommand.Parameters.AddWithValue("@total", total);
                                        insertCommand.Parameters.AddWithValue("@per", guests);

                                        await insertCommand.ExecuteNonQueryAsync();
                                    }

                                    // 다음 날로 이동
                                    day += 1;
                                }


                                // 시작일과 선택한 일 수로 예약 기간 생성
                                string startDate = $"{selectedMonth}월 {startDay}일";
                                string endDate = $"{month}월 {day - 1}일";
                                

                                await context.PostAsync($"{startDate}부터 {endDate}까지 예약되었습니다.");
                                await context.PostAsync($"총 가격은 {total}원입니다.");
                                // 카드 액션 추가
                                var messages = context.MakeMessage();        //Create message
                                var actions = new List<CardAction>();       //Create List

                                actions.Add(new CardAction() { Title = "더 예약하기", Value = "더 예약하기", Type = ActionTypes.ImBack });
                                actions.Add(new CardAction() { Title = "메인메뉴로 돌아가기", Value = "메인메뉴로 돌아가기", Type = ActionTypes.ImBack });

                                messages.Attachments.Add(                    //Create Hero Card & attachment
                                    new HeroCard { Title = "원하시는 메뉴를 선택해 주세요", Buttons = actions }.ToAttachment()
                                );

                                await context.PostAsync(messages);           //return our reply to the user

                                context.Wait(SendWelcomeMessageAsync);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await context.PostAsync("Error occurred...." + ex.Message);
            }
        }



        public async Task SendWelcomeMessageAsync(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;
            string strSelected = activity.Text.Replace(" ", "");

            if (strSelected == "더예약하기")
            {
                context.Call(new OrderDialog(), DialogResumeAfter);
            }
            else if (strSelected == "메인메뉴로돌아가기")
            {
                context.Call(new RootDialog(), DialogResumeAfter);
            }
            else
            {
                strMessage = "잘못 선택하셨습니다. 다시 선택해 주세요.";
                await context.PostAsync(strMessage);
                context.Wait(SendWelcomeMessageAsync);
            }
        }

        
        

        private async Task GetReservedDaysFromDatabase(IDialogContext context,string strOrder, int selectedMonth, int startDay, int rentDay)
        {
            List<int> reservedDays = new List<int>();

            try
            {
                // 데이터베이스에서 해당 호수의 예약된 날짜를 조회합니다.
                using (MySqlConnection connection = new MySqlConnection("Server=localhost;Port=3306;Database=rest;Uid=root;Pwd=rootpw"))
                {
                    await connection.OpenAsync();

                    string query = $"SELECT day FROM reserve WHERE name = '{strOrder}' AND month = {selectedMonth}";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {

                                int reservedDay = reader.GetInt32("day");
                                reservedDays.Add(reservedDay);
                            }
                        }
                    }
                }

                bool isConflict = false;
                for (int i = 0; i < rentDay; i++)
                {
                    if (reservedDays.Contains(startDay + i))
                    {
                        isConflict = true;
                        break;
                    }
                }


                if (isConflict)
                {
                    await context.PostAsync("선택한 날짜는 이미 예약되어 있는 날짜입니다. 다시 입력해주세요");

                    context.Wait(OnStartDaySelectedAsync);


                }
                else
                {
                    await context.PostAsync("휴대폰 번호를 입력해주세요 (11자리, '-' 제외)");
                    context.Wait(OnPhoneNumberReceivedAsync);
                }
                /*// 시작일로부터 rentDay 값만큼 날짜를 추가하고 중복 여부를 확인합니다.
                List<int> newReservedDays = new List<int>();
                for (int i = 0; i < rentDay; i++)
                {
                    int day = startDay + i;
                    if (reservedDays.Contains(day))
                    {
                        // 이미 예약된 날짜이므로 중복된 경우 처리할 작업을 수행합니다.
                        // 예를 들어, 중복된 날짜를 로그에 출력하거나 다른 로직을 수행할 수 있습니다.
                    }   
                    else
                    {
                        // 중복되지 않은 날짜이므로 리스트에 추가합니다.
                        newReservedDays.Add(day);
                    }
                }

                // 예약된 날짜 리스트를 반환합니다.
                return newReservedDays;
            }*/
            }
            catch (Exception ex)
            {
                Console.WriteLine("오류가 발생했습니다: " + ex.Message);
                
            }
        }
        public async Task DialogResumeAfter(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                
            }
            catch (TooManyAttemptsException)
            {
                await context.PostAsync("Error occurred....");
            }
        }

    }
}
