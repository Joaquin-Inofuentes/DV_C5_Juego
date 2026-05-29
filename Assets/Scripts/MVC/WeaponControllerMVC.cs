using UnityEngine;

namespace Game.MVC
{
    public class WeaponControllerMVC
    {
        private readonly WeaponModel model;
        private readonly WeaponView view;
        private readonly IWeaponInput input;

        public string IndicadorDeBalas { get; private set; }

        public WeaponControllerMVC(WeaponModel model, WeaponView view, IWeaponInput input)
        {
            this.model = model;
            this.view = view;
            this.input = input;

            if (this.model != null)
            {
                this.model.OnWeaponUpdated += HandleWeaponUpdated;
                this.model.OnAmmoUpdated += HandleAmmoUpdated;
            }

            // Initial updates
            HandleWeaponUpdated();
            HandleAmmoUpdated();
        }

        public void HandleUpdate(float deltaTime)
        {
            if (model == null || input == null) return;

            model.TiempoDesdeUltimoDisparo += deltaTime;

            // Cambio de armas
            int switchIndex = input.GetWeaponSwitchInput();
            if (switchIndex != -1)
            {
                model.SelectWeapon(switchIndex);
            }

            // Detección de Disparo
            bool isAutomatic = model.IsCurrentWeaponAutomatic();
            bool fireButtonPressed = input.GetFireInput(isAutomatic);

            if (fireButtonPressed && model.CanFire())
            {
                Disparar();
            }

            // Recarga
            if (input.GetReloadInput())
            {
                IntentarRecargar();
            }
        }

        private void Disparar()
        {
            if (model == null || view == null) return;

            model.ConsumeBullet();
            view.SpawnBala();
            view.PlaySound($"Disparo de {model.GetWeaponName()}");
            view.TriggerFlash();
            view.UpdateUI();

            if (model.BalasActuales <= 0)
            {
                IntentarRecargar();
            }
        }

        private void IntentarRecargar()
        {
            if (model == null || view == null) return;

            if (model.TryReload())
            {
                view.PlaySound($"Recarga de {model.GetWeaponName()}");
                view.UpdateUI();
            }
        }

        private void HandleWeaponUpdated()
        {
            if (model == null || view == null) return;

            view.UpdateActiveModels(model.NumeroDeArmaActual);
            view.SetProyectilDamage(model.GetCurrentWeaponDamage());
            view.UpdateUI();
        }

        private void HandleAmmoUpdated()
        {
            if (model == null || view == null) return;

            // Formato '06/30' con ceros a la izquierda (Formato "D2" o "00")
            IndicadorDeBalas = $"{model.BalasActuales:00}/{model.ReservaActual:00}";
            
            view.UpdateAmmoUI(IndicadorDeBalas);
        }

        public void Unsubscribe()
        {
            if (model != null)
            {
                model.OnWeaponUpdated -= HandleWeaponUpdated;
                model.OnAmmoUpdated -= HandleAmmoUpdated;
            }
        }
    }
}
