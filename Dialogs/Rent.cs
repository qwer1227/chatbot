using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;           //Add to process Async Task
using Microsoft.Bot.Connector;          //Add for Activity Class
using Microsoft.Bot.Builder.Dialogs;    //Add for Dialog Class
using System.Net.Http;                  //Add for internet
using GreatWall.Helpers;                //Add for CardHelper
using Microsoft.Bot.Builder.Scorables;
using System.Web.Services.Description;
using System.Net;
using GreatWall.DB;
using System.Data;
using GreatWall.RoomInfo;
using System.Web.Security;
using System.Security.Cryptography;
using GreatWall.DB;

namespace GreatWall
{
    [Serializable]
    public class RentDialog : IDialog<string>
    {
        static int OrderCHK = 0;
        string strMessage;
        string strOrder;
        string strServerUrl = "http://localhost:3984/Images/";
        int chkMonth; //선택월
        int MonthDay; //월의 최대 일
        int choiceDay; //시작일(선택일)
        string choiceRoom; //선택방
        int rentDays; //빌릴 일수
        int totalNumber; // 선택 인원
        int price; // 방의 기본요금
        int extraPostage; // 방마다 추가요금
        int totalPrice; // 최종 가격
        string phoneNumber;
        string password;
        DateTime today = DateTime.Today;
        static List<RoomInfo.RoomInfo> room = new List<RoomInfo.RoomInfo>();
        static RoomInfo.RoomInfo selectRoom; // 선택한 방 정보
        int MaxTotalNumber; //방의 최대 인원수
        static DBManager DBM = new DBManager("localhost", 3306, "root", "rootpw", "rest");


        public async Task StartAsync(IDialogContext context)
        {
            strMessage = null;
            strOrder = "[요일 선택] \n";
            DataTable test = DBM.ExecuteQuery("Select * from room");

            foreach (DataRow row in test.Rows)
            {
                RoomInfo.RoomInfo ri = new RoomInfo.RoomInfo();
                ri.name = Convert.ToString(row["name"]);
                ri.price = Convert.ToInt32(row["price"]); ;
                ri.max = Convert.ToInt32(row["max"]);
                ri.addprice = Convert.ToInt32(row["addprice"]);
                room.Add(ri);
            }

            //Called MessageReceivedAsync() without user input message
            await this.MessageReceivedAsync(context, null);
        }

        private async Task MessageReceivedAsync(IDialogContext context,
                                               IAwaitable<object> result)
        {

            if (result != null)
            {
                Activity activity = await result as Activity;
                activity.Text = activity.Text.Replace(" ", "");
                if (activity.Text == "예약완료")
                {
                    OrderCHK = 0;


                    if (DBM.ExecuteNonQuery("INSERT INTO reserve (name, month, day, rentday, phone, pwd,total, per) " +
                    $"VALUES('{choiceRoom}', {chkMonth}, {choiceDay}, {rentDays},'{phoneNumber}','{password}',{totalPrice},{totalNumber})"))
                    {
                        int insertCheckIndex = 1;
                        for (int idx = 0; idx < rentDays - 1; idx++)
                        {
                            int insertChoiceDay = choiceDay;
                            insertChoiceDay++;
                            if (insertChoiceDay > MonthDay)
                            {
                                DBM.ExecuteNonQuery("INSERT INTO reserve (name, month, day, rentday, phone, pwd,total, per) " +
                           $"VALUES('{choiceRoom}', {chkMonth + 1}, {insertCheckIndex}, {rentDays},'{phoneNumber}','{password}',{totalPrice},{totalNumber})");
                                insertCheckIndex++;
                            }
                            else
                            {
                                DBM.ExecuteNonQuery("INSERT INTO reserve (name, month, day, rentday, phone, pwd,total, per) " +
                        $"VALUES('{choiceRoom}', {chkMonth}, {insertChoiceDay}, {rentDays},'{phoneNumber}','{password}',{totalPrice},{totalNumber})");
                            }
                        }
                        await context.PostAsync("예약이 성공적으로 되었습니다.\r\n");
                        await context.PostAsync($"예약 정보 : {chkMonth}월 {choiceDay}일부터 {rentDays}일간 {choiceRoom} 입니다.");

                        var message = context.MakeMessage();
                        var actions = new List<CardAction>();

                        actions.Add(new CardAction() { Title = "메인 메뉴", Value = "메인메뉴", Type = ActionTypes.ImBack });
                        actions.Add(new CardAction() { Title = "더 예약하기", Value = "더예약하기", Type = ActionTypes.ImBack });

                        message.Attachments.Add(                    //Create Hero Card & attachment
                            new HeroCard { Title = "원하시는 메뉴를 선택해 주세요", Buttons = actions }.ToAttachment()
                        );

                        await context.PostAsync(message);           //return our reply to the user

                        context.Wait(SendWelcomeMessageAsync);
                        return;
                    }
                    else
                    {
                        await context.PostAsync("실패");
                    }
                }
                else if (activity.Text == "취소")
                {
                    OrderCHK = 0;
                    chkMonth = 0;
                    MonthDay = 0;
                    choiceDay = 0;
                    MaxTotalNumber = 0;
                    rentDays = 0;
                    totalNumber = 0;
                    price = 0;
                    extraPostage = 0;
                    totalPrice = 0;
                    phoneNumber = null;
                    password = null;
                    choiceRoom = null;
                }
                else
                {
                    if (OrderCHK == 0)
                    {
                        try
                        {
                            chkMonth = int.Parse(activity.Text);
                            if (chkMonth >= 13 || chkMonth <= 0)
                            {
                                await context.PostAsync("잘못 입력하셨습니다. 범위 내의 월만 숫자로 입력해 주세요");
                            }
                            else
                            {
                                OrderCHK++;

                            }
                        }
                        catch
                        {
                            await context.PostAsync("잘못 입력하셨습니다. 범위 내의 월만 숫자로 입력해 주세요");
                        }
                    }
                    else if (OrderCHK == 1)
                    {
                        if (activity.Text == "네")
                        {
                            await context.PostAsync("숙박 시작일을 선택해 주세요");
                            OrderCHK++;
                        }
                        else if (activity.Text == "아니요")
                        {
                            OrderCHK = 0;
                        }
                        else
                        {
                            await context.PostAsync("네, 아니오만 골라주세요");
                        }
                    }
                    else if (OrderCHK == 2)
                    {
                        try
                        {
                            choiceDay = int.Parse(activity.Text);
                            if (chkMonth % 2 == 0 && choiceDay > 30)
                            {
                                await context.PostAsync("잘못 입력하셨습니다. 정할수 있는 일로 선택해 주세요");
                            }
                            else if (chkMonth % 2 == 1 && choiceDay > 31)
                            {
                                await context.PostAsync("잘못 입력하셨습니다. 정할수 있는 일로 선택해 주세요");
                            }
                            else if (chkMonth == 2 && choiceDay > 28)
                            {
                                await context.PostAsync("잘못 입력하셨습니다. 정할수 있는 일로 선택해 주세요");
                            }
                            else
                            {
                                await context.PostAsync("선택하신 시작일은" + choiceDay + "일 입니다.");
                                OrderCHK++;

                            }
                        }
                        catch
                        {
                            await context.PostAsync("잘못 입력하셨습니다. 숫자만 입력해 주세요");
                        }
                    }
                    else if (OrderCHK == 3)
                    {
                        try
                        {
                            rentDays = int.Parse(activity.Text);
                            if (rentDays <= 14 && rentDays > 0)
                            {
                                await context.PostAsync("이용하실 일수는" + rentDays + "일 입니다.");
                                OrderCHK++;
                            }
                            else
                            {
                                await context.PostAsync("최대 14일까지 머무실 수 있습니다.");

                            }
                        }
                        catch
                        {
                            await context.PostAsync("잘못 입력하셨습니다. 숫자만 입력해 주세요");
                        }
                    }
                    else if (OrderCHK == 4)
                    {
                        bool chkRoom = false;
                        foreach (RoomInfo.RoomInfo ri in room)
                        {
                            if (ri.name == activity.Text)
                            {
                                selectRoom = ri;
                                choiceRoom = activity.Text;
                                await context.PostAsync("선택하신 방은" + choiceRoom + " 입니다.");

                                OrderCHK++;
                                chkRoom = true;
                                break;

                            }
                        }
                        if (!chkRoom)
                        {
                            await context.PostAsync("잘못 입력하셨습니다. 이용 가능하신 방에서만 골라주세요.");
                        }
                    }
                    else if (OrderCHK == 5)
                    {
                        if (activity.Text == "추가안함")
                        {
                            await context.PostAsync("추가인원을 선택하지 않으셨습니다.\r\n" +
                                "요금은" + price + "원 입니다.");
                            totalPrice = price;
                            OrderCHK++;
                        }
                        else
                        {
                            try
                            {
                                totalNumber = int.Parse(activity.Text);
                                if (selectRoom.max < (totalNumber + 1))
                                {
                                    await context.PostAsync("최대 추가 인원수를 초과했습니다. 다시 입력해 주세요");
                                }
                                else if (totalNumber <= 0)
                                {
                                    await context.PostAsync("추가인원을 선택하지 않으셨습니다.\r\n" +
                                    "요금은" + price + "원 입니다.");
                                    totalPrice = price;
                                    OrderCHK++;
                                }
                                else
                                {
                                    totalPrice = (((totalNumber) * selectRoom.addprice) + selectRoom.price) * rentDays;
                                    await context.PostAsync($"{totalNumber}명 추가하셔서 {totalNumber + 1}명 으로 예약하셨습니다.\r\n" +
                                        $" 추가 요금은 {(selectRoom.addprice * totalNumber) * rentDays}원 입니다. \r\n" +
                                        $" 최종 요금은 {totalPrice}원 입니다. ");
                                    OrderCHK++;
                                }
                            }
                            catch
                            {
                                await context.PostAsync("잘못 입력하셨습니다. 범위 내의 인원만 숫자로 입력해 주세요");
                            }
                        }
                    }
                    else if (OrderCHK == 6)
                    {
                        activity.Text = activity.Text.Replace("-", "");
                        activity.Text = activity.Text.Replace("/", "");
                        if (activity.Text.Length != 11)
                        {
                            await context.PostAsync("잘못 입력하셨습니다. 11자리 전화번호 정확하게 입력해 주세요");
                        }
                        else
                        {
                            phoneNumber = activity.Text;
                            OrderCHK++;
                        }
                    }
                    else if (OrderCHK == 7)
                    {
                        password = activity.Text;
                        OrderCHK++;

                    }


                }
                await this.MessageReceivedAsync(context, null);
            }
            else
            {
                if (OrderCHK == 0)
                {
                    strMessage = "날짜 선택";
                    await context.PostAsync(strMessage);    //return our reply to the user

                    var message = context.MakeMessage();                 //Create message      
                    var actions = new List<CardAction>();

                    actions.Add(new CardAction() { Title = "1월", Value = "1", Type = ActionTypes.ImBack });
                    actions.Add(new CardAction() { Title = "2월", Value = "2", Type = ActionTypes.ImBack });
                    actions.Add(new CardAction() { Title = "3월", Value = "3", Type = ActionTypes.ImBack });
                    actions.Add(new CardAction() { Title = "4월", Value = "4", Type = ActionTypes.ImBack });
                    actions.Add(new CardAction() { Title = "5월", Value = "5", Type = ActionTypes.ImBack });
                    actions.Add(new CardAction() { Title = "6월", Value = "6", Type = ActionTypes.ImBack });
                    actions.Add(new CardAction() { Title = "7월", Value = "7", Type = ActionTypes.ImBack });
                    actions.Add(new CardAction() { Title = "8월", Value = "8", Type = ActionTypes.ImBack });
                    actions.Add(new CardAction() { Title = "9월", Value = "9", Type = ActionTypes.ImBack });
                    actions.Add(new CardAction() { Title = "10월", Value = "10", Type = ActionTypes.ImBack });
                    actions.Add(new CardAction() { Title = "11월", Value = "11", Type = ActionTypes.ImBack });
                    actions.Add(new CardAction() { Title = "12월", Value = "12", Type = ActionTypes.ImBack });

                    message.Attachments.Add(
                    new HeroCard { Title = "원하시는 월을 고르세요", Buttons = actions }.ToAttachment()
                  );
                    await context.PostAsync(message);
                }
                else if (OrderCHK == 1)
                {
                    var message = context.MakeMessage();                 //Create message      
                    var actions = new List<CardAction>();
                    actions.Add(new CardAction() { Title = "네", Value = "네", Type = ActionTypes.ImBack });
                    actions.Add(new CardAction() { Title = "아니요", Value = "아니요", Type = ActionTypes.ImBack });
                    message.Attachments.Add(
                    new HeroCard { Title = chkMonth + "월로 선택하시겠습니까?", Buttons = actions }.ToAttachment()
                  );
                    await context.PostAsync(message);
                    if (chkMonth % 2 == 1)
                    {
                        MonthDay = 31;
                    }
                    else if (chkMonth == 2)
                    {
                        if (today.Year % 4 == 0)
                        {
                            MonthDay = 29;
                        }
                        else
                        {
                            MonthDay = 28;
                        }
                    }
                    else
                    {
                        MonthDay = 30;
                    }
                }
                else if (OrderCHK == 2)
                {
                    var message = context.MakeMessage();
                    var actions = new List<CardAction>();
                    for (int idx = 0; idx < MonthDay; idx++)
                    {
                        actions.Add(new CardAction() { Title = $"{idx + 1}일", Value = $"{idx + 1}", Type = ActionTypes.ImBack });
                    }
                    message.Attachments.Add(
                    new HeroCard { Title = "시작일을 고르세요", Buttons = actions }.ToAttachment());
                    await context.PostAsync(message);
                }
                else if (OrderCHK == 3)
                {

                    await context.PostAsync("머무실 기간을 입력해 주세요.\r\n 예시 : 2일~4일 -> 3일");
                }
                else if (OrderCHK == 4)
                {
                    //db에서 있는 호수 가져와서 noRoom<>에 있으면 삭제
                    //select로 해당 요일 choiceDay로 쿼리실행해서 나오는 호수는 Room 리스트에서 삭제

                    int roomcheckpoint = 0;
                    var message = context.MakeMessage();
                    var actions = new List<CardAction>();
                    int countingList = room.Count;
                    string[] roomname = new string[countingList];
                    DataTable test;
                    foreach (RoomInfo.RoomInfo ri in room)
                    {
                        roomname[roomcheckpoint] = ri.name;
                        roomcheckpoint++;
                    }
                    for (int index = 0; index <= choiceDay; index++)
                    {
                        if ((choiceDay + index) > MonthDay)
                        {
                            int up = 1;
                            test = DBM.ExecuteQuery($"Select name from reserve where month='{chkMonth + up}' AND day='{up}'");
                            up++;

                        }
                        else
                        {
                            test = DBM.ExecuteQuery($"Select name from reserve where month='{chkMonth}' AND day='{choiceDay + index}'");
                        }
                        if (test != null)
                        {
                            foreach (DataRow row in test.Rows)
                            {
                                for (int i = 0; i < countingList; i++)
                                {
                                    if (roomname[i] == Convert.ToString(row["name"]))
                                    {
                                        roomname[i] = null;
                                    }
                                }
                            }
                        }
                    }
                    for (int i = 0; i < countingList; i++)
                    {
                        if (roomname[i] != null)
                        {
                            actions.Add(new CardAction() { Title = $"{roomname[i]}", Value = $"{roomname[i]}", Type = ActionTypes.ImBack });
                        }
                    }

                    message.Attachments.Add(
                   new HeroCard { Title = "예약하실 방을 골라주세요", Buttons = actions }.ToAttachment());
                    await context.PostAsync(message);
                }
                else if (OrderCHK == 5)
                {
                    //db에서 있는 방 정보 가져와서 위에서 고른 방이 몇인 이용 가능과 가격을 먼저 출력.
                    var message = context.MakeMessage();
                    var actions = new List<CardAction>();
                    actions.Add(new CardAction() { Title = "추가안함", Value = "추가안함", Type = ActionTypes.ImBack });

                    foreach (RoomInfo.RoomInfo ri in room)
                    {
                        if (ri.name == choiceRoom)
                        {
                            for (int idx = 0; idx < (ri.max - 1); idx++)
                            {
                                actions.Add(new CardAction() { Title = $"{idx + 1}명 추가", Value = $"{idx + 1}", Type = ActionTypes.ImBack });
                            }
                            message.Attachments.Add(
                   new HeroCard
                   {
                       Title = $"{choiceRoom}의 기본 1일당 가격은 {ri.price}원 입니다. \r\n" +
                   $" 최대 선택 인원은 {ri.max}명 입니다.\r\n" +
                   $" 추가인원당 1일당 {ri.addprice}원 씩 추가됩니다.\r\n" +
                   $" 추가하실 인원수를 골라주세요.",
                       Buttons = actions
                   }.ToAttachment());
                        }
                    }
                    await context.PostAsync(message);
                }
                else if (OrderCHK == 6)
                {
                    await context.PostAsync($"{chkMonth}월 {choiceDay}일부터 {rentDays}일간 예약하시기를 원하시면 전화번호를 입력해 주세요.\r\n 취소하길 원하시면 아래의 버튼을 눌러주세요.(첫화면으로 돌아갑니다)");

                    var message = context.MakeMessage();
                    var actions = new List<CardAction>();

                    actions.Add(new CardAction() { Title = "취소", Value = "취소", Type = ActionTypes.ImBack });
                    message.Attachments.Add(
                   new HeroCard { Title = "", Buttons = actions }.ToAttachment());
                    await context.PostAsync(message);
                }
                else if (OrderCHK == 7)
                {
                    await context.PostAsync("사용하실 패스워드를 입력해 주세요.");
                }
                else if (OrderCHK == 8)
                {
                    await context.PostAsync($"예약을 완료하시려면 버튼 혹은 \'예약완료\'를 타이핑하세요.\r\n 취소하길 원하시면 아래의 버튼을 눌러주세요.(첫화면으로 돌아갑니다)");

                    var message = context.MakeMessage();
                    var actions = new List<CardAction>();

                    actions.Add(new CardAction() { Title = "예약완료", Value = "예약완료", Type = ActionTypes.ImBack });
                    actions.Add(new CardAction() { Title = "취소", Value = "취소", Type = ActionTypes.ImBack });

                    message.Attachments.Add(
                   new HeroCard
                   {
                       Title = "",
                       Buttons = actions
                   }.ToAttachment());
                    await context.PostAsync(message);

                }
                context.Wait(this.MessageReceivedAsync);
            }
        }

        public async Task SendWelcomeMessageAsync(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;
            string strSelected = activity.Text.Replace(" ", "");

            if (strSelected == "메인메뉴")
            {
                context.Call(new RootDialog(), DialogResumeAfter);
            }
            else if (strSelected == "더예약하기")
            {
                context.Call(new OrderDialog(), DialogResumeAfter);
            }
            else
            {
                strMessage = "잘못 선택하셨습니다. 다시 선택해 주세요.";
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
    }
}