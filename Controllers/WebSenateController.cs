using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MSPGeneratorWeb.Models;
using Microsoft.AspNet.Identity;


namespace MSPGeneratorWeb.Controllers
{
    /// <summary>
    /// <c>WebSenateController</c> - třída pro správu senátů přes web.
    /// </summary>
    /// <remarks><para>Dostupné pouze přihlášeným uživatelům.</para><para>Každý uživatel může spravovat jen ty senáty, které sám vytvořil.</para></remarks>
    [Authorize]
    public class WebSenateController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        /// <summary>
        /// <c>Index</c> (GET) - "webová" metoda pro zobrazení seznamu senátů + odkazy na akce (vytvoř, změň, smaž senát).
        /// </summary>
        /// <remarks>Models: WeSenateModels.cs, Views: WebSenate/Index.cshtml + _IndexPartial.cshtml.</remarks>
        // GET: WebSenate
        public ActionResult Index()
        {
            //return View(db.WebSenateModels.ToList()); //model: db.WebSenateModels.First(m => m.SenateCreator == uzivatelId));

            var uzivatelId = User.Identity.GetUserId();

            return View(db.WebSenateModels.Where(webSenateModel => webSenateModel.SenateCreator.Id == uzivatelId).OrderBy(webSenateModel => webSenateModel.SenateName).ToList()); 
            
            //model: db.WebSenateModels.First(m => m.SenateCreator == uzivatelId));

            /* var selSenates = db.WebSenateModels.Where(c => c.SenateCreator.Id == uzivatelId)
                    //.Select(c => c.NumCoefficientFix)
                    .OrderBy(c => c.SenateName)
                    .FirstOrDefault();`
            */
            
            //return View(db.WebSenateModels.OrderBy(webSenateModel => webSenateModel.SenateName).ToList()); 
        }


        /// <summary>
        /// <c>Create</c> (GET) - "webová" metoda pro vytvoření nového senátu.
        /// </summary>
        /// <remarks>Models: WeSenateModels.cs, Views: WebSenate/Create.cshtml.</remarks>
        // GET: WebSenate/Create
        public ActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// <c>Create</c> (POST) - "webová" metoda pro obsluhu formuláře na vytvoření nového senátu.
        /// </summary>
        /// <remarks>Models: WeSenateModels.cs, Views: WebSenate/Create.cshtml (nebo přesměruje na přehled senátů).</remarks>
        // POST: WebSenate/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "SenateName,Enabled,Load,Acases")] WebSenateModels webSenateModels)
        {
            /* if (webSenateModels == null)
            {
                throw new ArgumentNullException(nameof(webSenateModels));
            } */

            if (ModelState.IsValid)
            {
                //webSenateModels.SenateCreator_Id = UserID(); // cizí klíč v DB
                string currentUserId = User.Identity.GetUserId();
                ApplicationUser currentUser = db.Users.FirstOrDefault(x => x.Id == currentUserId);
                webSenateModels.SenateUserSet(currentUser); // cizí klíč v DB
                db.WebSenateModels.Add(webSenateModels);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(webSenateModels);
        }

        /// <summary>
        /// <c>Edit</c> (GET) - "webová" metoda pro změnu vybraného senátu.
        /// </summary>
        /// <remarks>Models: WeSenateModels.cs, Views: WebSenate/Edit.cshtml.</remarks>
        // GET: WebSenate/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            WebSenateModels webSenateModels = db.WebSenateModels.Find(id);
            if (webSenateModels == null)
            {
                return HttpNotFound();
            }
            return View(webSenateModels);
        }

        /// <summary>
        /// <c>Edit</c> (POST) - "webová" metoda pro obsluhu formuláře na změnu vybraného senátu.
        /// </summary>
        /// <remarks>Models: WeSenateModels.cs, Views: WebSenate/Edit.cshtml (při úspěchu přesměruje na seznam senátů).</remarks>
        // POST: WebSenate/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,SenateName,Enabled,Load,Acases")] WebSenateModels webSenateModels)
        {
            if (ModelState.IsValid)
            {
                db.Entry(webSenateModels).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(webSenateModels);
        }

        /// <summary>
        /// <c>Delete</c> (GET) - "webová" metoda pro smazání vybraného senátu.
        /// </summary>
        /// <remarks>Models: WeSenateModels.cs, Views: WebSenate/Delete.cshtml.</remarks>
        // GET: WebSenate/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            WebSenateModels webSenateModels = db.WebSenateModels.Find(id);
            if (webSenateModels == null)
            {
                return HttpNotFound();
            }
            return View(webSenateModels);
        }

        /// <summary>
        /// <c>Delete</c> (POST) - "webová" metoda pro obsluhu formuláře na odstranění vybraného senátu.
        /// </summary>
        /// <remarks>Models: WeSenateModels.cs, Views: WebSenate/Delete.cshtml.</remarks>
        // POST: WebSenate/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            WebSenateModels webSenateModels = db.WebSenateModels.Find(id);
            db.WebSenateModels.Remove(webSenateModels);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        
        /// <summary>
        /// <c>Dispose</c> - metoda vygenerovaná scaffoldingem z ASP.NET MVC.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
