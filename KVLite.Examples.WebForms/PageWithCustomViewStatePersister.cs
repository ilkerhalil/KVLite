using PommaLabs.KVLite.WebForms;
using System;
using System.Web.UI;

namespace PommaLabs.KVLite.Examples.WebForms
{
    /// <summary>
    ///   A page which defines a custom view state persister.
    /// </summary>
    public abstract class PageWithCustomViewStatePersister : Page
    {
        private const string ViewStateIdKey = "__PAGE_ID__";

        /// <summary>
        ///   Raises the <see cref="E:System.Web.UI.Control.Load"/> event.
        /// </summary>
        /// <param name="e">
        ///   The <see cref="T:System.EventArgs"/> object that contains the event data.
        /// </param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            InitViewStateId();
        }

        /// <summary>
        ///   Gets the <see cref="T:System.Web.UI.PageStatePersister"/> object associated with the page.
        /// </summary>
        /// <returns>
        ///   A <see cref="T:System.Web.UI.PageStatePersister"/> associated with the page.
        /// </returns>
        protected override sealed PageStatePersister PageStatePersister => new VolatileViewStatePersister(this);

        private void InitViewStateId()
        {
            var storedId = ViewState[ViewStateIdKey];
            if (storedId != null)
            {
                // ID is already stored, so we can return.
                return;
            }

            // We generate a new ID, we store it in the view state and then we can return.
            ViewState[ViewStateIdKey] = Guid.NewGuid();
        }
    }
}