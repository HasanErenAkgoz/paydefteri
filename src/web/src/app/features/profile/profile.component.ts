import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../shared/toast/toast.service';
import { apiErrorMessage } from '../../shared/utils/api-error';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
})
export class ProfileComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);

  readonly loading = signal(true);
  readonly savingProfile = signal(false);
  readonly savingPassword = signal(false);

  readonly email = signal('');
  readonly savedName = signal('');

  displayName = '';
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';

  readonly initials = computed(() => {
    const name = this.savedName().trim();
    if (name) {
      const parts = name.split(/\s+/).filter(Boolean);
      if (parts.length >= 2) {
        return (parts[0]![0]! + parts[1]![0]!).toUpperCase();
      }
      return name.slice(0, 2).toUpperCase();
    }
    const mail = this.email();
    return mail ? mail.slice(0, 2).toUpperCase() : 'PD';
  });

  ngOnInit(): void {
    this.auth.me().subscribe({
      next: (me) => {
        this.email.set(me.email);
        this.savedName.set(me.displayName);
        this.displayName = me.displayName;
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.toast.error(apiErrorMessage(err, 'Profil yüklenemedi.'));
      },
    });
  }

  saveProfile(): void {
    const name = this.displayName.trim();
    if (!name) {
      this.toast.error('Görünen ad zorunlu.');
      return;
    }
    this.savingProfile.set(true);
    this.auth.updateProfile(name).subscribe({
      next: () => {
        this.savingProfile.set(false);
        this.displayName = name;
        this.savedName.set(name);
        this.toast.success('Profil güncellendi.');
      },
      error: (err) => {
        this.savingProfile.set(false);
        this.toast.error(apiErrorMessage(err, 'Profil güncellenemedi.'));
      },
    });
  }

  savePassword(): void {
    if (!this.currentPassword || !this.newPassword) {
      this.toast.error('Mevcut ve yeni şifre gerekli.');
      return;
    }
    if (this.newPassword.length < 6) {
      this.toast.error('Yeni şifre en az 6 karakter olmalı.');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.toast.error('Yeni şifre tekrarı eşleşmiyor.');
      return;
    }
    this.savingPassword.set(true);
    this.auth.changePassword(this.currentPassword, this.newPassword).subscribe({
      next: () => {
        this.savingPassword.set(false);
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
        this.toast.success('Şifre güncellendi.');
      },
      error: (err) => {
        this.savingPassword.set(false);
        this.toast.error(apiErrorMessage(err, 'Şifre güncellenemedi.'));
      },
    });
  }
}
