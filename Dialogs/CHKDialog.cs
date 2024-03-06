using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Threading.Tasks;           //Add to process Async Task
using Microsoft.Bot.Connector;          //Add for Activity Class
using Microsoft.Bot.Builder.Dialogs;    //Add for Dialog Class
using System.Net.Http;                  //Add for internet
using System.Text;
using System.Data;
using GreatWall.DB;
using System.Runtime.Remoting.Contexts;

namespace GreatWall
{
    [Serializable]
    public class CHKDialog : IDialog<string>
    {

        string selectQuery;
        string deleteQuery;
        static DBManager dbManager = new DBManager("localhost", 3306, "root", "rootpw", "rest");
        string phoneNum;//휴대폰번호
        string password;//비밀번호
        List<int> num = new List<int>();// 에약테이블 고유번호
        StringBuilder responseBuilder = new StringBuilder();
        static DataRow row;
        public async Task StartAsync(IDialogContext context)
        {
            phoneNum = null;
            password = null;
            responseBuilder.Clear();
            
            await this.MessageReceivedAsync(context, null);
        }

        private async Task MessageReceivedAsync(IDialogContext context,
                                               IAwaitable<object> result)
        {
            if (result != null)
            {
                Activity activity = await result as Activity;
                if (activity.Text.Trim() == "교수님 쵝오")
                {
                    await context.PostAsync("ㅎㅇ관리자?");
                    selectQuery = "SELECT * FROM reserve";
                    SelectReserve(context, selectQuery);

                }
                else if (activity.Text.Trim() == "Exit")
                {
                    context.Done("Order Completed");
                }
                else if (num.Count>0 || activity.Text.Trim() == "뒤로가기")
                {
                    await HandleAction(context, activity.Text.Trim());
                }
                else
                {
                    if (phoneNum == null && CHKSelect($"SELECT * FROM reserve where phone = '{activity.Text.Trim()}'"))
                    {
                        //if() DB에서 예약 테이블에서 폰번호 count(*)이 0보다 크면 다음으로 넘어감
                        phoneNum = activity.Text.Trim();
                        await context.PostAsync("비밀번호를 입력해주세요: ");
                        context.Wait(this.MessageReceivedAsync);
                    }
                    else if (password == null && CHKSelect($"SELECT * FROM reserve where pwd = '{activity.Text.Trim()}'"))
                    {
                        //if () 비번이 널이면서 예약 테이블에서 where에 폰번호 비번 같은거 select로 싹다 읽으면서 리스트에 add함
                        //for문으로 리스트값들 쫘르륵 출력해줌.
                        password = activity.Text.Trim();
                        await context.PostAsync("로그인 성공!");
                        selectQuery = $"SELECT * FROM reserve where phone = '{phoneNum}' and pwd = '{password}'";
                        SelectReserve(context, selectQuery);


                    }
                    else
                    {
                        await context.PostAsync("일치하는 정보가 없습니다. 다시 입력해주세요: ");
                        context.Wait(this.MessageReceivedAsync);
                    }
                }
            }
            else
            {
                await context.PostAsync("휴대폰 번호를 입력해주세요: ");
                context.Wait(this.MessageReceivedAsync);
            }

        }
        public async Task HandleAction(IDialogContext context, string actionValue)
        {//예약취소 버튼 눌렀을시 실행
            if (actionValue.StartsWith("예약취소"))
            {
                //테이블 고유값 가져오기
                string reservationId = actionValue.Replace("예약취소", "").Trim();

                // 예약 취소 작업 수행
                try
                {
                    bool chk = false;
                    selectQuery = $"SELECT * FROM RESERVE WHERE id = {reservationId}";
                    DataTable dbTable;
                    dbTable = dbManager.ExecuteQuery(selectQuery);
                    int rentday = Convert.ToInt32(dbTable.Rows[0]["rentday"]);
                    for(int i=0; i<rentday; i++)
                    {
                        deleteQuery = $"DELETE FROM RESERVE WHERE id = {int.Parse(reservationId) + i}";
                        bool isDeleted = dbManager.ExecuteNonQuery(deleteQuery);
                        if (isDeleted)
                        {
                            chk = true;
                        }
                    }
                    if (chk)
                    {
                        await context.PostAsync($"예약 취소 되었습니다.");
                    }
                    else
                    {
                        await context.PostAsync($"없는 예약 번호입니다.");
                    }

                }
                catch 
                {
                    await context.PostAsync($"잘못 입력 하셨습니다.");
                }
                
                
                if (phoneNum != null && password != null)
                {
                    selectQuery = $"SELECT * FROM reserve where phone = '{phoneNum}' and pwd = '{password}'";
                }
                else
                {
                    selectQuery = "SELECT * FROM reserve";
                }

                    SelectReserve(context, selectQuery);
            }
            else if(actionValue.StartsWith("뒤로가기"))
            {
                context.Done("Order Completed");
            }
            else
            {
                await context.PostAsync($"잘못 입력 하셨습니다.");
                if (phoneNum != null && password != null)
                {
                    selectQuery = $"SELECT * FROM reserve where phone = '{phoneNum}' and pwd = '{password}'";
                }
                else
                {
                    selectQuery = "SELECT * FROM reserve";
                }

                SelectReserve(context, selectQuery);
            }
        }

        public void SelectReserve(IDialogContext context, string selectQuery)
        {
            num.Clear();
            responseBuilder.Clear();
            DataTable dbTable;
            dbTable = dbManager.ExecuteQuery(selectQuery);


            responseBuilder.AppendLine("예약번호 | 이름  | 인원수 | 예약월 | 예약일 | 렌트일 | 총가격 |");

            //관리자일경우에는 핸드폰 비번까지 알려주기
            for(int i=0; i<dbTable.Rows.Count; i++)//예약테이블 크기만큼 데이터 출력
            {
                row = dbTable.Rows[i];
                i += Convert.ToInt32(row["rentday"]) - 1;
                num.Add(Convert.ToInt32(row["id"]));
                responseBuilder.AppendLine($"{row["id"]} | {row["name"]} | {row["per"]}명 | {row["month"]}월 | {row["day"]}일 | {row["rentday"]} | {row["total"]} |");
            }

            string response = responseBuilder.ToString();

            var reply = context.MakeMessage();
            reply.Text = response;

            var actions = new List<CardAction>();

            //뒤로가기 버튼(초기화면)

            actions.Add(new CardAction
            {
                Title = $"뒤로가기",
                Type = ActionTypes.PostBack,
                Value = $"뒤로가기"
            });

            for (int i = 0; i < num.Count; i++)//예약테이블 크기만큼 예약취소 버튼 생성
            {
                int reservationId = num[i];  //실제로는 해당 값을 가져와야 함(예약 테이블 고유값)
                actions.Add(new CardAction
                {
                    Title = $"예약취소 {reservationId}",
                    Type = ActionTypes.PostBack,
                    Value = $"예약취소 {reservationId}"
                });
            }

            reply.SuggestedActions = new SuggestedActions { Actions = actions };

            context.PostAsync(reply);
        }
        public bool CHKSelect(string selectQuery)
        {
            
            DataTable dbTable = dbManager.ExecuteQuery(selectQuery);
            if (dbTable.Rows.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }
    }
}