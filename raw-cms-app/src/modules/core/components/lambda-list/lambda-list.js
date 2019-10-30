import { epicSpinners } from '../../../../utils/spinners.js';
import { snackbarService } from '../../services/snackbar-service.js';

const _LambdasList = async (res, rej) => {
  const tpl = await RawCMS.loadComponentTpl(
    '/modules/core/components/lambda-list/lambda-list.tpl.html'
  );

  res({
    components: {
      AtomSpinner: epicSpinners.AtomSpinner,
    },
    created: function() {
      this.fetchLambdas();
    },
    data: () => {
      return {
        isLoading: true,
        lambda: [],
        currentEntity: {},
        isDeleteConfirmVisible: false,
      };
    },
    methods: {
      fetchLambdas: async function() {
        //GET FROM web service
        this.lambda = [
          {
            _id: '123',
            Name: 'xccc',
            Path: '/sss',
            Code: "var x='y'",
            _meta_: { isDeleting: false },
          },
        ];
        this.isLoading = false;
        this.isDeleting = false;
      },
      goTo: function(entityId) {
        this.$router.push({ name: 'lambda-editor', params: { id: entityId } });
      },
      showDeleteConfirm: function(entity) {
        this.currentEntity = entity;
        this.isDeleteConfirmVisible = true;
      },
      dismissDeleteConfirm: function() {
        this.isDeleteConfirmVisible = false;
      },
      deleteEntity: async function(entity) {
        this.dismissDeleteConfirm();

        entity._meta_.isDeleting = true;
        const res = await lambdaSchemaService.deleteEntity(entity._id);

        entity._meta_.isDeleting = false;
        if (!res) {
          snackbarService.showMessage({
            color: 'error',
            message: this.$t('core.lambda.deleteErrorMsgTpl', {
              entityName: entity.CollectionName,
            }),
          });
          return;
        }

        this.lambda = this.lambda.filter(x => x._id !== entity._id);
        snackbarService.showMessage({
          color: 'success',
          message: this.$t('core.lambda.deletedMsgTpl', {
            entityName: entity.CollectionName,
          }),
        });
      },
    },
    template: tpl,
    watch: {
      $route: 'fetchData',
    },
  });
};

export const LambdasList = _LambdasList;
export default _LambdasList;
