using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PIA_Gimnasio.Pages
{
    public class PrivacidadModel : PageModel
    {
        private readonly ILogger<PrivacidadModel> _logger;

        public PrivacidadModel(ILogger<PrivacidadModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }

}
