﻿using AccountManager;
using BaseLib;
using BasePD;
using ScraperManagers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FollowManagers
{
    public class UnFollowManager
    {
        #region Variable

        public bool _IsfevoriteUnFollow = false;
        public int Nothread_UnFollow = 5;
        public bool isStopUnFollow = false;
        public List<Thread> lstThreadsUnFollow = new List<Thread>();
        public static int countThreadControllerUnFollow = 0;
        public static int UnFollowdata_count = 0;
        public int MaxUnFollowCount = 0;
        public readonly object UnFollowObjThread = new object();
        public int NOofDays = 0;
        public bool chkNoOFDays_UnFollow=false;

        public int minDelayUnFollow
        {
            get;
            set;
        }

        public int maxDelayUnFollow
        {
            get;
            set;
        }

        public int NoOfThreadsUnFollow
        {
            get;
            set;
        }

        string Unfollowuserid = string.Empty;
        List<string> lstNonFollowing = new List<string>();
        List<string> lstFollowers = new List<string>();
        List<string> lstFollowings = new List<string>();
        List<string> followings = new List<string>();
        List<string> NonFollowing = new List<string>();
        int NoOfPage = 10;
        int UnFollowCount = 0;

        #endregion
        Accounts ObjAccountManager = new Accounts();
        ScraperManager objScrape = new ScraperManager();

        public void StartUnFollow()
        {
            try
            {
                countThreadControllerUnFollow = 0;
                int numberOfAccountPatchUnFollow = 25;

                if (NoOfThreadsUnFollow > 0)
                {
                    numberOfAccountPatchUnFollow = NoOfThreadsUnFollow;
                }
                UnFollowdata_count = PDGlobals.loadedAccountsDictionary.Count();
                List<List<string>> list_listAccounts = new List<List<string>>();

                if (PDGlobals.listAccounts.Count >= 1)
                {
                    list_listAccounts = Utils.Utils.Split(PDGlobals.listAccounts, numberOfAccountPatchUnFollow);
                    foreach (List<string> listAccounts in list_listAccounts)
                    {
                        foreach (string account in listAccounts)
                        {
                            if (countThreadControllerUnFollow > Nothread_UnFollow)
                            {
                                try
                                {
                                    lock (UnFollowObjThread)
                                    {
                                        Monitor.Wait(UnFollowObjThread);
                                    }
                                }
                                catch (Exception Ex)
                                {

                                }
                            }

                            string acc = account.Split(':')[0];
                            PinInterestUser objPinInterestUser = null;
                            PDGlobals.loadedAccountsDictionary.TryGetValue(acc, out objPinInterestUser);
                            if (objPinInterestUser != null)
                            {
                                Thread profilerThread = new Thread(StartUnFollowMultiThreaded);
                                profilerThread.Name = "workerThread_Profiler_" + acc;
                                profilerThread.IsBackground = true;

                                profilerThread.Start(new object[] { objPinInterestUser });

                                countThreadControllerUnFollow++;
                            }
                        }

                    }
                }           
            }
            catch(Exception ex)
            {
                GlobusLogHelper.log.Error(" Error :" + ex.StackTrace);
            }
        }

        public void StartUnFollowMultiThreaded(object objparameter)
        {
            PinInterestUser objPinUser = new PinInterestUser();
            try
            {
                if (!isStopUnFollow)
                {
                    try
                    {
                        lstThreadsUnFollow.Add(Thread.CurrentThread);
                        lstThreadsUnFollow.Distinct().ToList();
                        Thread.CurrentThread.IsBackground = true;
                    }
                    catch (Exception ex)
                    { };

                    try
                    {
                        Array paramsArray = new object[1];
                        paramsArray = (Array)objparameter;
                        objPinUser = (PinInterestUser)paramsArray.GetValue(0);

                        #region Login

                        if (!objPinUser.isloggedin)
                        {
                            //Obj_AccountManager.httpHelper = httpHelper;
                            GlobusLogHelper.log.Info(" => [ Logging In With : " + objPinUser.Username + " ]");
                            bool checkLogin;

                            try
                            {
                                checkLogin = ObjAccountManager.LoginPinterestAccount1(ref objPinUser, objPinUser.Username, objPinUser.Password, objPinUser.ProxyAddress, objPinUser.ProxyPort, objPinUser.ProxyUsername, objPinUser.ProxyPassword, objPinUser.ScreenName);

                                string checklogin = objPinUser.globusHttpHelper.getHtmlfromUrl(new Uri("https://www.pinterest.com"));

                                if (!checkLogin)
                                {
                                    try
                                    {
                                        checkLogin = ObjAccountManager.LoginPinterestAccount1forlee(ref objPinUser, objPinUser.Username, objPinUser.Password, objPinUser.ProxyAddress, objPinUser.ProxyPort, objPinUser.ProxyUsername, objPinUser.ProxyPassword, objPinUser.ScreenName);

                                    }
                                    catch { };
                                    if (!checkLogin)
                                    {
                                        GlobusLogHelper.log.Info(" => [ Logging UnSuccessfull : " + objPinUser.Username + " ]");
                                        return;
                                    }
                                }
                            }

                            catch { };
                        }
                        #endregion

                        GlobusLogHelper.log.Info(" => [ Logged In With : " + objPinUser.Username + " ]");
                        StartActionMultithreadUnFollow(ref objPinUser);

                    }
                    catch (Exception ex)
                    {
                        GlobusLogHelper.log.Error("Error :" + ex.StackTrace);
                    }

                }
            }
            catch(Exception ex)
            {
                GlobusLogHelper.log.Error(" Error :" + ex.StackTrace);
            }
        }

        public void  StartActionMultithreadUnFollow(ref PinInterestUser objPinUser)
        {
            try
            {
                string ScreenName = ObjAccountManager.Getscreen_NameRepin(ref objPinUser);

                //lstFollowers.Distinct().ToList();

                //foreach (string lst in followings)
                //{
                //    lstFollowings.Add(lst);
                //}


                if (!chkNoOFDays_UnFollow)
                {
                    List<string> lstUsers = new List<string>();
                    try
                    {
                        lstUsers = objScrape.GetUserFollowing_new(ScreenName, NoOfPage, Globals.followingCountLogin);
                        lstUsers.Reverse();
                        lstUsers = lstUsers.Distinct().ToList();
                        if (lstUsers.Count > 0)
                        {
                            ClGlobul.lstFollowing_UnFollow.AddRange(lstUsers);
                        }

                        ClGlobul.lstFollowing_UnFollow = ClGlobul.lstFollowing_UnFollow.Distinct().ToList();
                    }
                    catch (Exception ex)
                    {
                        GlobusLogHelper.log.Info("[ " + DateTime.Now + " ] => [ Not Fetched User List ]");
                        //GlobusFileHelper.AppendStringToTextfileNewLine("Error --> StartUserScraperMultiThreaded() --> " + ex.Message, ApplicationData.ErrorLogFile);
                    }

                    //lstNonFollowing = lstUsers.Except(lstFollowers).ToList();
                    lstNonFollowing.AddRange(lstUsers);
                }
                else if (chkNoOFDays_UnFollow == true)
                {                  
                    clsSettingDB DataBase = new clsSettingDB();
                    DataTable dt = DataBase.SelectUnfollowsToday(objPinUser.Username);
                    foreach (DataRow rd in dt.Rows)
                    {
                        DateTime dt1 = DateTime.Parse(rd[3].ToString());
                        DateTime dt2 = DateTime.Today;
                        TimeSpan span = dt2 - dt1;
                        int DayDiffrence = (int)span.Days;
                        if (DayDiffrence >= NOofDays)
                        {
                            NonFollowing.Add("http://pinterest.com/" + rd[2].ToString() + "/");
                        }
                    }
                    lstNonFollowing = NonFollowing.Distinct().ToList();
                }

                if (lstNonFollowing.Count() > 0)
                {
                    foreach (string UnFollowUrl in lstNonFollowing)
                    {
                        string url = string.Empty;
                        try
                        {                          
                            if (!UnFollowUrl.Contains("http://pinterest.com"))
                            {
                                url = "https://pinterest.com/" + UnFollowUrl + "/";
                            }
                            else
                            {
                                url = UnFollowUrl;
                            }
                   
                            bool IsUnFollowed = UnFollow_New(url, ref objPinUser);

                            if (IsUnFollowed)
                            {
                                GlobusLogHelper.log.Info(" => [ Unfollowed : " + url + " From " + objPinUser.Username + " ]");
                                //UnFollowCount++;
                                MaxUnFollowCount--;
                            }
                            else
                            {
                                GlobusLogHelper.log.Info(" => [ Not Unfollowed : " + url + " From " + objPinUser.Username + " ]");
                            }

                            int Delay = RandomNumberGenerator.GenerateRandom(minDelayUnFollow, maxDelayUnFollow);
                            GlobusLogHelper.log.Info(" => [ Delay For " + Delay + " Seconds ]");
                            Thread.Sleep(Delay * 1000);

                        }
                        catch (Exception ex)
                        {
                           // GlobusFileHelper.AppendStringToTextfileNewLine("Error --> UnFollowUserMultiThreaded() 1--> " + ex.Message, ApplicationData.ErrorLogFile);
                        }
                        if (MaxUnFollowCount == 0)
                        {
                            break;
                        }

                    }                 

                }
                else
                {
                    GlobusLogHelper.log.Info(" => [ No Users to UnFollow ]");
                }
            }
            catch(Exception ex)
            {
                GlobusLogHelper.log.Error(" Error :" + ex.StackTrace);
            }
            finally
            {
                try
                {
                    if (countThreadControllerUnFollow > Nothread_UnFollow)
                    {
                        lock (UnFollowObjThread)
                        {
                            Monitor.Pulse(UnFollowObjThread);
                        }
                        UnFollowdata_count--;
                    }

                }
                catch (Exception ex)
                {
                    GlobusLogHelper.log.Error(" Error : " + ex.StackTrace);
                }
                countThreadControllerUnFollow--;

                if (MaxUnFollowCount == 0)  //|| DivideByUserinput < 0)                
                {
                    GlobusLogHelper.log.Info(" [ PROCESS COMPLETED  ] ");
                    GlobusLogHelper.log.Info("------------------------------------------------------");

                }
            }
        }

        public bool UnFollow_New(string UserUrl, ref PinInterestUser objPinUserManager)
        {
            try
            {

                if (ObjAccountManager.LoginPinterestAccount(ref objPinUserManager))
                {
                    string UnFollowPageSource = objPinUserManager.globusHttpHelper.getHtmlfromUrlProxy(new Uri(UserUrl), "", "", 80, "", "", objPinUserManager.UserAgent);
                                   
                    try
                    {
                        int startindex = UnFollowPageSource.IndexOf("\"user_id\":");
                        string start = UnFollowPageSource.Substring(startindex).Replace("\"user_id\":", "");
                        int endindex = start.IndexOf("\", \"");
                        string end = start.Substring(0, endindex);
                        Unfollowuserid = end.Replace("\"", "");
                    }
                    catch (Exception ex)
                    {

                    }
                    string UnfollowuserName = UserUrl.Split('/')[UserUrl.Split('/').Count() - 2];

                    // string PostData = "source_url=%2F" + pinterestAccountManager.Name + "%2Ffollowers%2F&data=%7B%22options%22%3A%7B%22user_id%22%3A%22" + Unfollowuserid.Trim() + "%22%7D%2C%22context%22%3A%7B%22app_version%22%3A%22" + pinterestAccountManager.App_version + "%22%7D%7D&module_path=App()%3EUserProfilePage(resource%3DUserResource(username%3D" + pinterestAccountManager.Name + "))%3EUserProfileContent(resource%3DUserResource(username%3D" + pinterestAccountManager.Name + "))%3EGrid(resource%3DUserFollowersResource(username%3D" + pinterestAccountManager.Name + "))%3EGridItems(resource%3DUserFollowersResource(username%3D" + pinterestAccountManager.Name + "))%3EUser(resource%3DUserResource(username%3D" + UnfollowuserName + "))%3EUserFollowButton(followed%3Dtrue%2C+is_me%3Dfalse%2C+unfollow_text%3DUnfollow%2C+follow_ga_category%3Duser_follow%2C+unfollow_ga_category%3Duser_unfollow%2C+disabled%3Dfalse%2C+color%3Ddim%2C+tagName%3Dbutton%2C+text%3DUnfollow%2C+user_id%3D" + Unfollowuserid + "%2C+follow_text%3DFollow%2C+follow_class%3Ddefault)";
                    //  PostData = "source_url=%2F" + UnfollowuserName + "%2F&data=%7B%22options%22%3A%7B%22user_id%22%3A%22" + Unfollowuserid.Trim() + "%22%7D%2C%22context%22%3A%7B%22app_version%22%3A%22" + pinterestAccountManager.App_version + "%22%2C%22https_exp%22%3Afalse%7D%7D&module_path=App()%3EUserProfilePage(resource%3DUserResource(username%3D" + UnfollowuserName + "))%3EUserInfoBar(resource%3DUserResource(username%3D" + UnfollowuserName + "))%3EUserFollowButton(followed%3Dtrue%2C+is_me%3Dfalse%2C+unfollow_text%3DUnfollow+All%2C+memo%3D%5Bobject+Object%5D%2C+follow_ga_category%3Duser_follow%2C+unfollow_ga_category%3Duser_unfollow%2C+disabled%3Dfalse%2C+color%3Dprimary%2C+text%3DFollow+All%2C+user_id%3D" + Unfollowuserid.Replace(" ", "") + "%2C+follow_text%3DFollow+All%2C+follow_class%3Dprimary)";


                    // string PostData = "source_url=%2F" + UnfollowuserName + "%2F&data=%7B%22options%22%3A%7B%22user_id%22%3A%22" + Unfollowuserid.Trim() + "%22%7D%2C%22context%22%3A%7B%7D%7D&module_path=App()%3EUserProfilePage(resource%3DUserResource(username%3D" + UnfollowuserName + "%2C+invite_code%3Dnull))%3EUserProfileHeader(resource%3DUserResource(username%3D" + UnfollowuserName + "%2C+invite_code%3Dnull))%3EUserFollowButton(follow_text%3DFollow%2C+unfollow_text%3DUnfollow%2C+followed%3Dtrue%2C+user_id%3D2" + Unfollowuserid.Trim() + "%2C+memo%3D%5Bobject+Object%5D%2C+text%3DUnfollow%2C+color%3Ddim%2C+disabled%3Dfalse%2C+is_me%3Dfalse%2C+follow_class%3Dprimary%2C+follow_ga_category%3Duser_follow%2C+unfollow_ga_category%3Duser_unfollow)";
                    string PostData = "source_url=%2F" + UnfollowuserName + "%2Ffollowing%2F&data=%7B%22options%22%3A%7B%22user_id%22%3A%22" + Unfollowuserid.Trim() + "%22%7D%2C%22context%22%3A%7B%7D%7D&module_path=App%3EUserProfilePage%3EUserProfileContent%3EUserProfileFollowingGrid%3EGrid%3EGridItems%3EUser%3EUserFollowButton(user_id%3D" + Unfollowuserid.Trim() + "%2C+follow_class%3Ddefault%2C+followed%3Dtrue%2C+class_name%3DgridItem%2C+log_element_type%3D62%2C+text%3DUnfollow%2C+color%3Ddim%2C+disabled%3Dfalse%2C+follow_text%3DFollow%2C+unfollow_text%3DUnfollow%2C+is_me%3Dfalse%2C+follow_ga_category%3Duser_follow%2C+unfollow_ga_category%3Duser_unfollow)";

                    try
                    {
                        UnFollowPageSource = objPinUserManager.globusHttpHelper.postFormDataProxyPin(new Uri("https://www.pinterest.com/resource/UserFollowResource/delete/"), PostData, "https://www.pinterest.com/" + objPinUserManager.Username);
                    }
                    catch(Exception ex)
                    {
                        GlobusLogHelper.log.Error(" Error :" + ex.StackTrace);
                    }


                    if (UnFollowPageSource.Contains("\"error\": null"))
                    {
                        GlobusLogHelper.log.Info("[ " + DateTime.Now + " ] => [ Successfully UnFollow For this User " + objPinUserManager.Username + " ]");
                        return true;
                    }
                    else if (UnFollowPageSource.Contains("We're unable to complete this request. Your account is currently in read-only mode to protect your pins. You must reset your password to continue pinning."))
                    {
                        GlobusLogHelper.log.Info("[ " + DateTime.Now + " ] => [ UnFollow Process Failed For this User " + objPinUserManager.Username + "--> ]");
                        GlobusLogHelper.log.Info("[ " + DateTime.Now + " ] => [ We're unable to complete this request. Your account is currently in read-only mode to protect your pins. ]");
                        GlobusLogHelper.log.Info("[ " + DateTime.Now + " ] => [ You must reset your password to continue pinning. ]");
                        return false;
                    }
                    else
                    {
                        GlobusLogHelper.log.Info("[ " + DateTime.Now + " ] => [ UnFollow Process Failed For this User " + objPinUserManager.Username + " ]");
                        return false;
                    }
                }
                else
                {
                    GlobusLogHelper.log.Info("[ " + DateTime.Now + " ] => [ Login Issue with " + objPinUserManager.Username + " ]");
                    return false;
                }

            }
            catch (Exception ex)
            {
                GlobusLogHelper.log.Info("[ " + DateTime.Now + " ] => [ UnFollow Process Failed For this User " + objPinUserManager.Username + " ]");
                return false;
            }
        }



    }
}