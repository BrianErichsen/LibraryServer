//Author: Brian Erichsen Fagundes
//CS6016 - Summer - 2024
//Database Systems and Applications
//Professor: Nabil Makarem

using LibraryWebServer.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo( "TestProject1" )]
namespace LibraryWebServer.Controllers
{
    public class HomeController : Controller
    {

        // WARNING:
        // This very simple web server is designed to be as tiny and simple as possible
        // This is NOT the way to save user data.
        // This will only allow one user of the web server at a time (aside from major security concerns).
        private static string user = "";
        private static int card = -1;

        private readonly LibraryContext db;
        // protected override void Dispose(bool disposing)
        // {
        //     if (disposing) {
        //         db.Dispose();
        //     }
        //     base.Dispose(disposing);
        // }
        public HomeController(LibraryContext _db)
        {
           db = _db;
        }

        /// <summary>
        /// Given a Patron name and CardNum, verify that they exist and match in the database.
        /// If the login is successful, sets the global variables "user" and "card"
        /// </summary>
        /// <param name="name">The Patron's name</param>
        /// <param name="cardnum">The Patron's card number</param>
        /// <returns>A JSON object with a single field: "success" with a boolean value:
        /// true if the login is accepted, false otherwise.
        /// </returns>
        [HttpPost]
        public IActionResult CheckLogin( string name, int cardnum )
        {
            bool loginSuccessful = false;
            //simple query to return given patron witn name and cardnum
            var patron = from p in db.Patrons
                        where p.Name == name && p.CardNum == cardnum
                        select p;
            //if returned patron is a valid data from table
            // where Patrons table with given name and CardNum
            bool isValidPatron = patron.Any();
            //then set login success to true
            if (isValidPatron) {
                loginSuccessful = true;
            }

            if ( !loginSuccessful )
            {
                return Json( new { success = false } );
            }
            else
            {
                user = name;
                card = cardnum;
                return Json( new { success = true } );
            }
        }

        /// <summary>
        /// Logs a user out. This is implemented for you.
        /// </summary>
        /// <returns>Success</returns>
        [HttpPost]
        public ActionResult LogOut()
        {
            user = "";//sets user to empty and card num to a invalid num
            card = -1;
            return Json( new { success = true } );
        }

        /// <summary>
        /// Returns a JSON array representing all known books.
        /// Each book should contain the following fields:
        /// {"isbn" (string), "title" (string), "author" (string), "serial" (uint?), "name" (string)}
        /// Every object in the list should have isbn, title, and author.
        /// Books that are not in the Library's inventory (such as Dune) should have a null serial. 'left join'
        /// The "name" field is the name of the Patron who currently has the book checked out (if any)
        /// Books that are not checked out should have an empty string "" for name.
        /// </summary>
        /// <returns>The JSON representation of the books</returns>
        [HttpPost]
        public ActionResult AllTitles()
        {
            var query =
                from t in db.Titles.DefaultIfEmpty()
                join i in db.Inventory
                    on t.Isbn equals i.Isbn
                    into titles_inventory

                from j1 in titles_inventory.DefaultIfEmpty()
                join c in db.CheckedOut
                    on j1.Serial equals c.Serial
                    into j1_CheckOut
                
                from j2 in j1_CheckOut.DefaultIfEmpty()
                join p in db.Patrons
                    on j2.CardNum equals p.CardNum
                    into j2_Patrons

                from j3 in j2_Patrons.DefaultIfEmpty()
                select new
                {
                    isbn = t.Isbn,
                    title = t.Title,
                    author = t.Author,
                    serial = j1 != null ? (uint?)j1.Serial : null,
                    name = j3.Name??""
                };
            return Json(query.ToArray());
        }

        /// <summary>
        /// Returns a JSON array representing all books checked out by the logged in user 
        /// The logged in user is tracked by the global variable "card".
        /// Every object in the array should contain the following fields:
        /// {"title" (string), "author" (string), "serial" (uint) (note this is not a nullable uint) }
        /// Every object in the list should have a valid (non-null) value for each field.
        /// </summary>
        /// <returns>The JSON representation of the books</returns>
        [HttpPost]
        public ActionResult ListMyBooks()
        {
            var query =
                from c in db.CheckedOut
                join i in db.Inventory
                    on c.Serial equals i.Serial
                    into checkedOut_inventory

                from j1 in checkedOut_inventory
                join t in db.Titles
                    on j1.Isbn equals t.Isbn
                    into j1_Titles
                
                from j2 in j1_Titles
                where c.CardNum == card
                select new
                {
                    title = j2.Title,
                    author = j2.Author,
                    serial = j1.Serial
                };
            return Json(query);
        }


        /// <summary>
        /// Updates the database to represent that
        /// the given book is checked out by the logged in user (global variable "card").
        /// In other words, insert a row into the CheckedOut table.
        /// You can assume that the book is not currently checked out by anyone.
        /// </summary>
        /// <param name="serial">The serial number of the book to check out</param>
        /// <returns>success</returns>
        [HttpPost]
        public ActionResult CheckOutBook( int serial )
        {
            var checkedOut = new CheckedOut();
            checkedOut.Serial = (uint)serial;
            checkedOut.CardNum = (uint)card;

            db.CheckedOut.Add(checkedOut);
            try {
                db.SaveChanges();
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            return Json( new { success = true } );
        }

        /// <summary>
        /// Returns a book currently checked out by the logged in user (global variable "card").
        /// In other words, removes a row from the CheckedOut table.
        /// You can assume the book is checked out by the user.
        /// </summary>
        /// <param name="serial">The serial number of the book to return</param>
        /// <returns>Success</returns>
        [HttpPost]
        public ActionResult ReturnBook( int serial )
        {
            var checkedOut = new CheckedOut();
            checkedOut.Serial = (uint)serial;
            checkedOut.CardNum = (uint)card;

            if (checkedOut != null) {
                db.CheckedOut.Remove(checkedOut);
                db.SaveChanges();
                return Json( new { success = true } );
            } else {
                return Json(new { success = false, message = "Invalid return book entry!"});
            }
        }


        /*******************************************/
        /****** Do not modify below this line ******/
        /*******************************************/


        public IActionResult Index()
        {
            if ( user == "" && card == -1 )
                return View( "Login" );

            return View();
        }


        /// <summary>
        /// Return the Login page.
        /// </summary>
        /// <returns></returns>
        public IActionResult Login()
        {
            user = "";
            card = -1;

            ViewData["Message"] = "Please login.";

            return View();
        }

        /// <summary>
        /// Return the MyBooks page.
        /// </summary>
        /// <returns></returns>
        public IActionResult MyBooks()
        {
            if ( user == "" && card == -1 )
                return View( "Login" );

            return View();
        }



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache( Duration = 0, Location = ResponseCacheLocation.None, NoStore = true )]
        public IActionResult Error()
        {
            return View( new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier } );
        }
    }
}