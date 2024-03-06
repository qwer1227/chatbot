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
            await context.PostAsync("�������� �������ּ���:");
            context.Wait(OnStartDaySelectedAsync);
        }

        private async Task OnStartDaySelectedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            startDay = message.Text.Trim();

            // �������� ������ ��, �� ���� �������� �����
            await context.PostAsync($"{startDay}���� �� �� ���� �����Ͻðڽ��ϱ�?");
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
                        await context.PostAsync("���� ������ �ִ� �뿩 �Ⱓ�� 14���Դϴ�.");
                        context.Wait(OnRentDayReceivedAsync);
                        return;
                    }

                    // ����ڰ� ������ �����ϰ� ���� �Ⱓ�� ������� ���� ������ ������ ����մϴ�.
                    int startDayValue = int.Parse(startDay);
                    int endDayValue = startDayValue + rentDay - 1;



                    

                    // �����ͺ��̽����� �ش� ȣ���� ����� ��¥�� ��ȸ�մϴ�.

                    await GetReservedDaysFromDatabase(context,strOrder, selectedMonth, startDayValue, rentDay);
                    // ���� ������ ���� ���� �̹� ����� ��¥�� �ִ��� Ȯ���մϴ�.
                    

                    // ���� �ڵ� ����...

                    // ���� ������ ���� �ȿ� �ִ� ���, �޴��� ��ȣ�� �Է¹޴� �ܰ�� �����մϴ�.
                    
                }
                else
                {
                    await context.PostAsync("�ùٸ� ���ڸ� �Է����ּ���.");
                    context.Wait(OnRentDayReceivedAsync);
                }
            }
            catch (Exception ex)
            {
                await context.PostAsync("������ �߻��߽��ϴ�: " + ex.Message);
            }
        }

        private async Task OnPhoneNumberReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            string phone = message.Text.Trim();

            if (phone.Length == 11) // �޴��� ��ȣ�� 11�ڸ����� Ȯ��
            {
                // �޴��� ��ȣ�� �Է¹��� ��, ��й�ȣ�� �Է¹���
                await context.PostAsync("��й�ȣ�� �Է����ּ���:");

                context.PrivateConversationData.SetValue<string>("phone", phone);

                context.Wait(OnPasswordReceivedAsync); // ��й�ȣ�� �Է¹޴� �޼���� �̵�
            }
            else
            {
                await context.PostAsync("�ùٸ� �޴��� ��ȣ�� �Է����ּ��� (11�ڸ�, '-' ����)");
                context.Wait(OnPhoneNumberReceivedAsync);
            }
        }

        private async Task OnPasswordReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            string password = message.Text.Trim();

            // state���� �޴��� ��ȣ ��������
            string phone = context.PrivateConversationData.GetValueOrDefault<string>("phone");
            try
            {
                // DB�� ���� ����
                using (MySqlConnection connection = new MySqlConnection("Server=localhost;Port=3306;Database=rest;Uid=root;Pwd=rootpw"))
                {
                    await connection.OpenAsync();

                    // �� ���� ��ȸ
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
                                // �����ϰ� ������ �� ���� ���� �Ⱓ ����
                                int startDayValue = int.Parse(startDay);
                                int endDayValue = startDayValue + rentDay - 1;

                                // �ο��� �߰� �ݾ� ���
                                int additionalPricePerPerson = 0;
                                int guests = numberOfGuests; // ����ڰ� �Է��� �ο� ��
                                if (guests > totalGuests)
                                {
                                    additionalPricePerPerson = (guests - totalGuests) * additionalPrice;
                                }

                                // �� ���� ���
                                int total = basePrice + guests * additionalPricePerPerson;
                                total = total * rentDay;
                                int day = int.Parse(startDay);
                                int month = selectedMonth;

                                for (int i = 0; i < rentDay; i++)
                                {
                                    // ������ ���� Ȧ�� ���� ���
                                    if (month % 2 == 1)
                                    {
                                        // 31�ϱ��� �ִ� ���̸鼭 �������� 31���� �Ѿ�� ���
                                        if (month <= 7 && day > 31)
                                        {
                                            month += 1;
                                            day = 1;
                                        }
                                        // 30�ϱ��� �ִ� ���̸鼭 �������� 30���� �Ѿ�� ���
                                        else if (month > 7 && day > 30)
                                        {
                                            month += 1;
                                            day = 1;
                                        }
                                    }
                                    // ������ ���� ¦�� ���� ���
                                    else
                                    {
                                        // 30�ϱ��� �ִ� ���̸鼭 �������� 30���� �Ѿ�� ���
                                        if (month <= 7 && day > 30)
                                        {
                                            month += 1;
                                            day = 1;
                                        }
                                        // 31�ϱ��� �ִ� ���̸鼭 �������� 31���� �Ѿ�� ���
                                        else if (month > 7 && day > 31)
                                        {
                                            month += 1;
                                            day = 1;
                                        }
                                    }

                                    // 2���� ���
                                    if (month == 2)
                                    {
                                        // �������� 28���� �Ѿ�� ���
                                        if (day > 28)
                                        {
                                            month += 1;
                                            day = 1;
                                        }
                                    }

                                    // INSERT ���� ����
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

                                    // ���� ���� �̵�
                                    day += 1;
                                }


                                // �����ϰ� ������ �� ���� ���� �Ⱓ ����
                                string startDate = $"{selectedMonth}�� {startDay}��";
                                string endDate = $"{month}�� {day - 1}��";
                                

                                await context.PostAsync($"{startDate}���� {endDate}���� ����Ǿ����ϴ�.");
                                await context.PostAsync($"�� ������ {total}���Դϴ�.");
                                // ī�� �׼� �߰�
                                var messages = context.MakeMessage();        //Create message
                                var actions = new List<CardAction>();       //Create List

                                actions.Add(new CardAction() { Title = "�� �����ϱ�", Value = "�� �����ϱ�", Type = ActionTypes.ImBack });
                                actions.Add(new CardAction() { Title = "���θ޴��� ���ư���", Value = "���θ޴��� ���ư���", Type = ActionTypes.ImBack });

                                messages.Attachments.Add(                    //Create Hero Card & attachment
                                    new HeroCard { Title = "���Ͻô� �޴��� ������ �ּ���", Buttons = actions }.ToAttachment()
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

            if (strSelected == "�������ϱ�")
            {
                context.Call(new OrderDialog(), DialogResumeAfter);
            }
            else if (strSelected == "���θ޴��ε��ư���")
            {
                context.Call(new RootDialog(), DialogResumeAfter);
            }
            else
            {
                strMessage = "�߸� �����ϼ̽��ϴ�. �ٽ� ������ �ּ���.";
                await context.PostAsync(strMessage);
                context.Wait(SendWelcomeMessageAsync);
            }
        }

        
        

        private async Task GetReservedDaysFromDatabase(IDialogContext context,string strOrder, int selectedMonth, int startDay, int rentDay)
        {
            List<int> reservedDays = new List<int>();

            try
            {
                // �����ͺ��̽����� �ش� ȣ���� ����� ��¥�� ��ȸ�մϴ�.
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
                    await context.PostAsync("������ ��¥�� �̹� ����Ǿ� �ִ� ��¥�Դϴ�. �ٽ� �Է����ּ���");

                    context.Wait(OnStartDaySelectedAsync);


                }
                else
                {
                    await context.PostAsync("�޴��� ��ȣ�� �Է����ּ��� (11�ڸ�, '-' ����)");
                    context.Wait(OnPhoneNumberReceivedAsync);
                }
                /*// �����Ϸκ��� rentDay ����ŭ ��¥�� �߰��ϰ� �ߺ� ���θ� Ȯ���մϴ�.
                List<int> newReservedDays = new List<int>();
                for (int i = 0; i < rentDay; i++)
                {
                    int day = startDay + i;
                    if (reservedDays.Contains(day))
                    {
                        // �̹� ����� ��¥�̹Ƿ� �ߺ��� ��� ó���� �۾��� �����մϴ�.
                        // ���� ���, �ߺ��� ��¥�� �α׿� ����ϰų� �ٸ� ������ ������ �� �ֽ��ϴ�.
                    }   
                    else
                    {
                        // �ߺ����� ���� ��¥�̹Ƿ� ����Ʈ�� �߰��մϴ�.
                        newReservedDays.Add(day);
                    }
                }

                // ����� ��¥ ����Ʈ�� ��ȯ�մϴ�.
                return newReservedDays;
            }*/
            }
            catch (Exception ex)
            {
                Console.WriteLine("������ �߻��߽��ϴ�: " + ex.Message);
                
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
